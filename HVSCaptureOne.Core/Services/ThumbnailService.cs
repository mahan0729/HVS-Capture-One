using System.Diagnostics;
using Serilog;

namespace HVSCaptureOne.Core.Services;

/// <summary>
/// Extracts JPEG thumbnail images from an MP4 file at specified timestamps using FFmpeg.
/// Thumbnails are written as 1280×720 JPGs with letterbox/pillarbox padding for non-16:9 content.
/// </summary>
public class ThumbnailService
{
    private readonly string _ffmpegPath;

    /// <summary>
    /// Initializes ThumbnailService with the full path to the ffmpeg executable.
    /// </summary>
    /// <returns></returns>
    public ThumbnailService(string ffmpegPath)
    {
        if (!File.Exists(ffmpegPath))
            throw new FileNotFoundException($"ffmpeg not found at: {ffmpegPath}");

        _ffmpegPath = ffmpegPath;
    }

    /// <summary>
    /// Extracts one thumbnail per timestamp from the source video.
    /// Output files are named {baseName}_thumb_001.jpg, _002.jpg, etc., written to outputDir.
    /// Returns the full paths of all successfully extracted thumbnails.
    /// Non-fatal: individual frame failures are logged and skipped.
    /// </summary>
    /// <param name="sourcePath">Full path to the source MP4.</param>
    /// <param name="outputDir">Directory where thumbnail JPGs are written (created if absent).</param>
    /// <param name="baseName">Filename stem used to name the output JPGs (no extension).</param>
    /// <param name="timestamps">Ordered list of timestamps at which to extract frames.</param>
    /// <returns>List of full paths to the successfully extracted thumbnail files.</returns>
    public List<string> Extract(
        string           sourcePath,
        string           outputDir,
        string           baseName,
        List<TimeSpan>   timestamps)
    {
        Directory.CreateDirectory(outputDir);

        var paths = new List<string>();

        for (int i = 0; i < timestamps.Count; i++)
        {
            string outputPath = Path.Combine(outputDir, $"{baseName}_thumb_{i + 1:D3}.jpg");

            try
            {
                ExtractFrame(sourcePath, timestamps[i], outputPath);
                paths.Add(outputPath);
                Log.Debug("Thumbnail {Index}/{Total} extracted: {Path}", i + 1, timestamps.Count, outputPath);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to extract thumbnail {Index} at {Time}", i + 1, timestamps[i]);
            }
        }

        Log.Information("Thumbnail extraction complete. {Extracted}/{Total} thumbnails written to {Dir}",
            paths.Count, timestamps.Count, outputDir);

        return paths;
    }

    /// <summary>
    /// Runs FFmpeg to extract a single frame at the given timestamp and write it as a JPEG.
    /// Uses fast keyframe seek (-ss before -i). Output is scaled to 1280×720 with black padding.
    /// </summary>
    /// <returns></returns>
    private void ExtractFrame(string sourcePath, TimeSpan timestamp, string outputPath)
    {
        // Format as HH:MM:SS.mmm for FFmpeg -ss seek.
        string seekTime = $"{(int)timestamp.TotalHours:D2}:{timestamp.Minutes:D2}:{timestamp.Seconds:D2}.{timestamp.Milliseconds:D3}";

        // scale=1280:720:force_original_aspect_ratio=decrease pads non-16:9 content with black bars.
        string scaleFilter = "scale=1280:720:force_original_aspect_ratio=decrease,pad=1280:720:(ow-iw)/2:(oh-ih)/2:black";

        var psi = new ProcessStartInfo
        {
            FileName  = _ffmpegPath,
            Arguments = $"-y -ss {seekTime} -i \"{sourcePath}\" -vframes 1 -vf \"{scaleFilter}\" -q:v 2 \"{outputPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute  = false,
            CreateNoWindow   = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start ffmpeg process.");

        // Drain both streams to prevent pipe-buffer deadlock.
        var stdoutTask = Task.Run(() => process.StandardOutput.ReadToEnd());
        var stderrTask = Task.Run(() => process.StandardError.ReadToEnd());
        process.WaitForExit();

        stdoutTask.Wait();
        stderrTask.Wait();

        if (process.ExitCode != 0)
        {
            string error = stderrTask.Result;
            throw new InvalidOperationException(
                $"ffmpeg exited with code {process.ExitCode} extracting frame at {seekTime}: {error}");
        }
    }
}
