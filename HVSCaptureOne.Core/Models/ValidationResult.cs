namespace HVSCaptureOne.Core.Models;

/// <summary>
/// Represents the outcome of a file validation check performed by ValidationService.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// True if the file passed all MVP validation rules; false otherwise.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Human-readable description of why validation failed.
    /// Empty string when IsValid is true.
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// Returns a successful ValidationResult with no error message.
    /// </summary>
    /// <returns></returns>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Returns a failed ValidationResult with the given reason shown to the user.
    /// </summary>
    /// <returns></returns>
    public static ValidationResult Fail(string reason) =>
        new() { IsValid = false, ErrorMessage = reason };
}
