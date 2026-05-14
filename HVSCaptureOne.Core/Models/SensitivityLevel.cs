namespace HVSCaptureOne.Core.Models;

/// <summary>
/// Controls how aggressively FFmpeg scene detection fires on the source video.
/// Lower thresholds catch more scene changes (higher false-positive rate on tape noise);
/// higher thresholds only fire on strong, obvious cuts.
/// </summary>
public enum SensitivityLevel
{
    /// <summary>
    /// Detects subtle scene changes. Use for clean digital sources.
    /// Higher false-positive rate on VHS/Hi8/MiniDV noise.
    /// Maps to scdet threshold 3.0.
    /// </summary>
    Aggressive = 1,

    /// <summary>
    /// Balanced detection. Recommended default for most digitized tape content.
    /// Maps to scdet threshold 8.0.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Only fires on strong, obvious hard cuts. Best for very noisy tape stock.
    /// Maps to scdet threshold 15.0.
    /// </summary>
    Minimal = 3
}
