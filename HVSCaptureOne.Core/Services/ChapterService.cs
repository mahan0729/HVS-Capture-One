namespace HVSCaptureOne.Core.Services;

/// <summary>
/// Generates auto-chapter timestamps for a video asset.
/// v1.5 rule: one chapter every 5 minutes starting at 00:00:00:00,
/// for each interval strictly less than the video duration.
/// </summary>
public static class ChapterService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Returns a list of HH:MM:SS:FF timestamp strings, one per 5-minute interval,
    /// beginning at 00:00:00:00 and continuing while the timestamp is less than duration.
    /// Returns an empty list when duration is zero or negative.
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
    /// Formats a TimeSpan as HH:MM:SS:FF where FF is always 00
    /// (auto-generated chapters align to whole minutes only).
    /// </summary>
    /// <returns>Timecode string in HH:MM:SS:FF format.</returns>
    private static string FormatTimecode(TimeSpan ts)
        => $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}:00";
}
