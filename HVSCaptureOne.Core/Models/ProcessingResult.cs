namespace HVSCaptureOne.Core.Models;

/// <summary>
/// Represents the outcome of a video processing operation.
/// </summary>
public class ProcessingResult
{
    /// <summary>True when the output file was written successfully.</summary>
    public bool Success { get; init; }

    /// <summary>Full path to the written output file. Populated on success.</summary>
    public string OutputPath { get; init; } = string.Empty;

    /// <summary>Human-readable error description. Populated on failure.</summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// Creates a successful result with the given output path.
    /// </summary>
    /// <returns></returns>
    public static ProcessingResult Ok(string outputPath) =>
        new() { Success = true, OutputPath = outputPath };

    /// <summary>
    /// Creates a failed result with the given error message.
    /// </summary>
    /// <returns></returns>
    public static ProcessingResult Fail(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
