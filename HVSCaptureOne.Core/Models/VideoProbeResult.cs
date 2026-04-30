namespace HVSCaptureOne.Core.Models;

/// <summary>
/// Holds the raw data returned by FFprobe for a given video file.
/// Populated by FFprobeService.Probe() and consumed by ValidationService.
/// </summary>
public class VideoProbeResult
{
    /// <summary>
    /// Full path to the source file that was probed.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Short codec name as reported by FFprobe (e.g. "h264", "hevc").
    /// </summary>
    public string CodecName { get; init; } = string.Empty;

    /// <summary>
    /// Long codec description (e.g. "H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10").
    /// </summary>
    public string CodecLongName { get; init; } = string.Empty;

    /// <summary>
    /// Video frame width in pixels.
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    /// Video frame height in pixels.
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    /// Pixel format (e.g. "yuv420p").
    /// </summary>
    public string PixelFormat { get; init; } = string.Empty;

    /// <summary>
    /// Average frame rate in frames per second, parsed from FFprobe's avg_frame_rate
    /// rational string (e.g. "30000/1001" → 29.97).
    /// </summary>
    public double FrameRate { get; init; }

    /// <summary>
    /// Real base frame rate from FFprobe's r_frame_rate field.
    /// Compared against FrameRate to detect variable frame rate (VFR) files.
    /// </summary>
    public double RFrameRate { get; init; }

    /// <summary>
    /// Total duration of the video.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Container format name(s) as reported by FFprobe (e.g. "mov,mp4,m4a,3gp,3g2,mj2").
    /// </summary>
    public string FormatName { get; init; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSizeBytes { get; init; }
}
