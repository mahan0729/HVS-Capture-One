namespace HVSCaptureOne.Core.Services;

/// <summary>
/// Converts chapter timestamps into HH:MM:SS:FF atom strings.
/// v1.5: 5-minute interval generation (Generate).
/// v2+: converts scene-detection output from SceneDetectionService (FromTimestamps).
/// </summary>
public static class ChapterService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Returns a list of HH:MM:SS:FF timestamp strings, one per 5-minute interval,
    /// beginning at 00:00:00:00 and continuing while the timestamp is less than duration.
    /// Returns an empty list when duration is zero or negative.
    /// Used as the fallback when scene detection is unavailable.
    /// </summary>
    /// <returns>Ordered list of chapter timestamp strings in HH:MM:SS:FF format.</returns>
    public static List<string> Generate(TimeSpan duration)
    {
        var chapters = new List<string>();
        var ts = TimeSpan.Zero;

        while (ts < duration)
        {
            chapters.Add(FormatTimecode(ts));
            ts += Interval;
        }

        return chapters;
    }

    /// <summary>
    /// Converts a list of TimeSpan scene timestamps from SceneDetectionService
    /// into HH:MM:SS:FF atom strings. FF is always 00 — scene times are approximate
    /// and do not carry sub-frame precision.
    /// </summary>
    /// <param name="timestamps">Ordered list of scene-change timestamps.</param>
    /// <returns>Ordered list of chapter timestamp strings in HH:MM:SS:FF format.</returns>
    public static List<string> FromTimestamps(List<TimeSpan> timestamps)
        => timestamps.Select(FormatTimecode).ToList();

    /// <summary>
    /// Formats a TimeSpan as HH:MM:SS:FF where FF is always 00.
    /// </summary>
    /// <returns>Timecode string in HH:MM:SS:FF format.</returns>
    private static string FormatTimecode(TimeSpan ts)
        => $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}:00";
}
