namespace HVSCaptureOne.Core.Models;

public class VideoMetadata
{
    // ── User-entered fields ──────────────────────────────────────────────
    public string MainTitle { get; set; } = string.Empty;       // ttl1 + dvat
    public string SubTitle { get; set; } = string.Empty;        // ttls
    public string Description { get; set; } = string.Empty;     // ttld
    public string OriginalTapeId { get; set; } = string.Empty;  // display only

    // Internal studio note — never embedded in the MP4
    public string Notes { get; set; } = string.Empty;

    // ── Auto-detected by FFprobe (M2) ────────────────────────────────────
    public TimeSpan DetectedDuration { get; set; }
    public string DetectedResolution { get; set; } = string.Empty;
    public string DetectedCodec { get; set; } = string.Empty;

    // ── Convenience copies from Project for atom mapping ─────────────────
    public string ClientName { get; set; } = string.Empty;      // unam
    public string ClientEmail { get; set; } = string.Empty;     // emal

    // ── Chapters (v1.5+) ─────────────────────────────────────────────────
    // Each entry is a timestamp string in HH:MM:SS:FF format (e.g. "00:04:38:09")
    public List<string> Chapters { get; set; } = new();
}
