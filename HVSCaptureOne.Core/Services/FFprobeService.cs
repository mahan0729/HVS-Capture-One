using System.Diagnostics;
using System.Text.Json;
using HVSCaptureOne.Core.Models;

namespace HVSCaptureOne.Core.Services;

/// <summary>
/// Runs FFprobe against an MP4 file and returns structured probe data.
/// The path to ffprobe.exe is supplied at construction time by the App layer.
/// </summary>
public class FFprobeService
{
    private readonly string _ffprobePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Initializes FFprobeService with the full path to the ffprobe executable.
    /// </summary>
    /// <returns></returns>
    public FFprobeService(string ffprobePath)
    {
        if (!File.Exists(ffprobePath))
            throw new FileNotFoundException($"ffprobe not found at: {ffprobePath}");

        _ffprobePath = ffprobePath;
    }

    /// <summary>
    /// Runs FFprobe on the given file and returns a VideoProbeResult with
    /// codec, resolution, frame rate, duration, and format information.
    /// Throws InvalidOperationException if the file cannot be read or FFprobe fails.
    /// </summary>
    /// <returns></returns>
    public VideoProbeResult Probe(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Source file not found: {filePath}");

        string json = RunFFprobe(filePath);
        return ParseProbeOutput(json, filePath);
    }

    /// <summary>
    /// Populates the detected technical fields on a VideoMetadata object
    /// from a completed VideoProbeResult.
    /// </summary>
    /// <returns></returns>
    public void PopulateMetadata(VideoProbeResult probe, VideoMetadata metadata)
    {
        metadata.DetectedDuration = probe.Duration;
        metadata.DetectedResolution = $"{probe.Width}x{probe.Height}";
        metadata.DetectedCodec = probe.CodecName;
    }

    /// <summary>
    /// Executes FFprobe with JSON output flags and returns the raw stdout string.
    /// </summary>
    /// <returns></returns>
    private string RunFFprobe(string filePath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _ffprobePath,
            Arguments = $"-v quiet -print_format json -show_streams -show_format \"{filePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ffprobe process.");

        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            string error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"ffprobe exited with code {process.ExitCode}: {error}");
        }

        return output;
    }

    /// <summary>
    /// Parses the raw FFprobe JSON output into a VideoProbeResult.
    /// </summary>
    /// <returns></returns>
    private static VideoProbeResult ParseProbeOutput(string json, string filePath)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Find the first video stream
        var videoStream = FindVideoStream(root)
            ?? throw new InvalidOperationException("No video stream found in file.");

        var format = root.GetProperty("format");

        string avgFrameRate = videoStream.TryGetProperty("avg_frame_rate", out var afr)
            ? afr.GetString() ?? "0/0" : "0/0";

        string rFrameRate = videoStream.TryGetProperty("r_frame_rate", out var rfr)
            ? rfr.GetString() ?? "0/0" : "0/0";

        double durationSeconds = format.TryGetProperty("duration", out var dur)
            ? double.TryParse(dur.GetString(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double d) ? d : 0
            : 0;

        long fileSize = format.TryGetProperty("size", out var sz)
            ? long.TryParse(sz.GetString(), out long s) ? s : 0
            : 0;

        return new VideoProbeResult
        {
            FilePath = filePath,
            CodecName = videoStream.TryGetProperty("codec_name", out var cn)
                ? cn.GetString() ?? string.Empty : string.Empty,
            CodecLongName = videoStream.TryGetProperty("codec_long_name", out var cln)
                ? cln.GetString() ?? string.Empty : string.Empty,
            Width = videoStream.TryGetProperty("width", out var w) ? w.GetInt32() : 0,
            Height = videoStream.TryGetProperty("height", out var h) ? h.GetInt32() : 0,
            PixelFormat = videoStream.TryGetProperty("pix_fmt", out var pf)
                ? pf.GetString() ?? string.Empty : string.Empty,
            FrameRate = ParseFrameRate(avgFrameRate),
            RFrameRate = ParseFrameRate(rFrameRate),
            Duration = TimeSpan.FromSeconds(durationSeconds),
            FormatName = format.TryGetProperty("format_name", out var fn)
                ? fn.GetString() ?? string.Empty : string.Empty,
            FileSizeBytes = fileSize
        };
    }

    /// <summary>
    /// Finds the first video stream element in the FFprobe JSON streams array.
    /// Returns null if no video stream is present.
    /// </summary>
    /// <returns></returns>
    private static JsonElement? FindVideoStream(JsonElement root)
    {
        if (!root.TryGetProperty("streams", out var streams))
            return null;

        foreach (var stream in streams.EnumerateArray())
        {
            if (stream.TryGetProperty("codec_type", out var ct) &&
                ct.GetString() == "video")
                return stream;
        }

        return null;
    }

    /// <summary>
    /// Parses an FFprobe rational frame rate string (e.g. "30000/1001") into a double.
    /// Returns 0.0 for invalid or zero-denominator values.
    /// </summary>
    /// <returns></returns>
    private static double ParseFrameRate(string rational)
    {
        if (string.IsNullOrWhiteSpace(rational)) return 0.0;

        var parts = rational.Split('/');
        if (parts.Length != 2) return 0.0;

        if (!double.TryParse(parts[0], out double num)) return 0.0;
        if (!double.TryParse(parts[1], out double den) || den == 0) return 0.0;

        return num / den;
    }
}
