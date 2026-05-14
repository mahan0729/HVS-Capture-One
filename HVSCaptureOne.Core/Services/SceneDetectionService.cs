using System.Diagnostics;
using System.Text.RegularExpressions;
using HVSCaptureOne.Core.Models;
using Serilog;

namespace HVSCaptureOne.Core.Services;

/// <summary>
/// Runs FFmpeg scene detection on an MP4 file and returns a list of scene-change timestamps.
/// Uses the FFmpeg scdet filter. Falls back to 5-minute intervals if detection fails or
/// produces no results.
/// </summary>
public class SceneDetectionService
{
    private readonly string _ffmpegPath;

    // Minimum gap between consecutive chapter points.
    // Scenes detected closer together than this are merged (only the earlier one is kept).
    private static readonly TimeSpan MinSceneGap = TimeSpan.FromSeconds(30);

    // Maximum number of chapters the atom schema supports.
    private const int MaxChapters = 99;

    // Regex to extract pts_time value from FFmpeg scdet stderr output.
    // Line format: "... lavfi.scd.score: X, pts: N, pts_time: T, key_frame: K"
    private static readonly Regex PtsTimeRegex =
        new(@"pts_time:\s*([\d.]+)", RegexOptions.Compiled);

    // scdet threshold per sensitivity level (lower = more detections).
    private static readonly Dictionary<SensitivityLevel, double> Thresholds = new()
    {
        [SensitivityLevel.Aggressive] = 3.0,
        [SensitivityLevel.Medium]     = 8.0,
        [SensitivityLevel.Minimal]    = 15.0,
    };

    /// <summary>
    /// Initializes SceneDetectionService with the full path to the ffmpeg executable.
    /// </summary>
    /// <returns></returns>
    public SceneDetectionService(string ffmpegPath)
    {
        if (!File.Exists(ffmpegPath))
            throw new FileNotFoundException($"ffmpeg not found at: {ffmpegPath}");

        _ffmpegPath = ffmpegPath;
    }

    /// <summary>
    /// Runs FFmpeg scdet on the given source file and returns a list of scene-change timestamps.
    /// The list always begins with TimeSpan.Zero. Timestamps closer than 30 seconds are merged.
    /// Capped at 99 results. On any failure, falls back to 5-minute intervals.
    /// </summary>
    /// <param name="sourcePath">Full path to the source MP4.</param>
    /// <param name="sensitivity">How aggressively to detect scene changes.</param>
    /// <param name="duration">Total video duration — used for the fallback calculation.</param>
    /// <returns>Ordered list of scene-change timestamps, always starting at TimeSpan.Zero.</returns>
    public List<TimeSpan> Detect(string sourcePath, SensitivityLevel sensitivity, TimeSpan duration)
    {
        Log.Information(
            "Scene detection started. Sensitivity={Sensitivity} Source={Source}",
            sensitivity, sourcePath);

        try
        {
            double threshold = Thresholds[sensitivity];
            string stderr    = RunScdet(sourcePath, threshold);
            var    raw       = ParseTimestamps(stderr);

            var timestamps = EnforceMinGap(raw);
            if (timestamps.Count == 0)
                timestamps.Insert(0, TimeSpan.Zero);

            if (timestamps[0] != TimeSpan.Zero)
                timestamps.Insert(0, TimeSpan.Zero);

            if (timestamps.Count > MaxChapters)
                timestamps = timestamps.Take(MaxChapters).ToList();

            // If detection ran but found no scene changes, fall back to 5-minute intervals
            if (timestamps.Count == 1)
            {
                Log.Information(
                    "Scene detection found no cuts — falling back to 5-minute intervals (sensitivity={Sensitivity})",
                    sensitivity);
                return Fallback(duration);
            }

            Log.Information(
                "Scene detection complete. Detected={Count} chapters (sensitivity={Sensitivity})",
                timestamps.Count, sensitivity);

            return timestamps;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Scene detection failed — falling back to 5-minute intervals");
            return Fallback(duration);
        }
    }

    /// <summary>
    /// Executes the FFmpeg scdet filter and returns raw stderr output.
    /// </summary>
    /// <returns>Raw stderr string from FFmpeg.</returns>
    private string RunScdet(string sourcePath, double threshold)
    {
        // Use -v info so scdet lines appear in stderr.
        string thresholdStr = threshold.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        var psi = new ProcessStartInfo
        {
            FileName  = _ffmpegPath,
            Arguments = $"-v info -i \"{sourcePath}\" -vf \"scdet=threshold={thresholdStr}\" -f null -",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute  = false,
            CreateNoWindow   = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ffmpeg process.");

        // Read both streams concurrently to prevent pipe-buffer deadlock.
        var stdoutTask = Task.Run(() => process.StandardOutput.ReadToEnd());
        var stderrTask = Task.Run(() => process.StandardError.ReadToEnd());
        process.WaitForExit();

        stdoutTask.Wait();
        string stderr = stderrTask.Result;

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"ffmpeg exited with code {process.ExitCode}");

        return stderr;
    }

    /// <summary>
    /// Parses scdet timestamps from raw FFmpeg stderr output.
    /// Extracts pts_time values from lines produced by the scdet filter.
    /// </summary>
    /// <returns>Unsorted, unfiltered list of detected scene-change timestamps.</returns>
    private static List<TimeSpan> ParseTimestamps(string stderr)
    {
        var timestamps = new List<TimeSpan>();

        foreach (string line in stderr.Split('\n'))
        {
            // scdet lines contain both "scd.score" and "pts_time"
            if (!line.Contains("scd.score", StringComparison.Ordinal))
                continue;

            var match = PtsTimeRegex.Match(line);
            if (!match.Success)
                continue;

            if (double.TryParse(match.Groups[1].Value,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double seconds))
            {
                timestamps.Add(TimeSpan.FromSeconds(seconds));
            }
        }

        timestamps.Sort();
        return timestamps;
    }

    /// <summary>
    /// Removes timestamps that fall within MinSceneGap of the preceding one.
    /// Ensures chapters are at least 30 seconds apart.
    /// </summary>
    /// <returns>Filtered list with minimum 30-second spacing.</returns>
    private static List<TimeSpan> EnforceMinGap(List<TimeSpan> timestamps)
    {
        if (timestamps.Count == 0) return timestamps;

        var result = new List<TimeSpan> { timestamps[0] };

        for (int i = 1; i < timestamps.Count; i++)
        {
            if (timestamps[i] - result[^1] >= MinSceneGap)
                result.Add(timestamps[i]);
        }

        return result;
    }

    /// <summary>
    /// Generates fallback chapters at 5-minute intervals when scene detection fails.
    /// </summary>
    /// <returns>List of timestamps spaced 5 minutes apart starting from zero.</returns>
    private static List<TimeSpan> Fallback(TimeSpan duration)
    {
        var result   = new List<TimeSpan>();
        var interval = TimeSpan.FromMinutes(5);
        var ts       = TimeSpan.Zero;

        while (ts < duration)
        {
            result.Add(ts);
            ts += interval;
        }

        return result;
    }
}
