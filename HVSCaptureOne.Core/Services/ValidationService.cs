using HVSCaptureOne.Core.Models;

namespace HVSCaptureOne.Core.Services;

/// <summary>
/// Validates a VideoProbeResult against the HVS Capture One MVP acceptance rules.
/// Any file that fails validation must not proceed to processing.
/// </summary>
public class ValidationService
{
    private const int MaxWidth = 3840;
    private const int MaxHeight = 2160;
    private const double VfrTolerance = 0.01;

    /// <summary>
    /// Validates the given probe result against all MVP rules.
    /// Returns ValidationResult.Success() if the file is acceptable,
    /// or ValidationResult.Fail() with a user-facing message if not.
    /// Rules are checked in order — the first failure is returned immediately.
    /// </summary>
    /// <returns></returns>
    public ValidationResult Validate(VideoProbeResult probe)
    {
        var containerCheck = ValidateContainer(probe);
        if (!containerCheck.IsValid) return containerCheck;

        var codecCheck = ValidateCodec(probe);
        if (!codecCheck.IsValid) return codecCheck;

        var resolutionCheck = ValidateResolution(probe);
        if (!resolutionCheck.IsValid) return resolutionCheck;

        var orientationCheck = ValidateOrientation(probe);
        if (!orientationCheck.IsValid) return orientationCheck;

        var vfrCheck = ValidateFrameRate(probe);
        if (!vfrCheck.IsValid) return vfrCheck;

        return ValidationResult.Success();
    }

    /// <summary>
    /// Checks that the file is an MP4 container.
    /// </summary>
    /// <returns></returns>
    private static ValidationResult ValidateContainer(VideoProbeResult probe)
    {
        if (probe.FormatName.Contains("mp4", StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Success();

        return ValidationResult.Fail(
            "Unsupported file format. Only MP4 files are accepted.");
    }

    /// <summary>
    /// Checks that the video codec is H.264.
    /// </summary>
    /// <returns></returns>
    private static ValidationResult ValidateCodec(VideoProbeResult probe)
    {
        if (probe.CodecName.Equals("h264", StringComparison.OrdinalIgnoreCase))
            return ValidationResult.Success();

        return ValidationResult.Fail(
            $"Unsupported codec '{probe.CodecName}'. Only H.264 video is accepted in Version 1.");
    }

    /// <summary>
    /// Checks that the video resolution does not exceed 4K limits.
    /// </summary>
    /// <returns></returns>
    private static ValidationResult ValidateResolution(VideoProbeResult probe)
    {
        if (probe.Width <= MaxWidth && probe.Height <= MaxHeight)
            return ValidationResult.Success();

        return ValidationResult.Fail(
            $"4K resolution ({probe.Width}x{probe.Height}) is not supported in Version 1.");
    }

    /// <summary>
    /// Checks that the video is not vertical (portrait orientation).
    /// </summary>
    /// <returns></returns>
    private static ValidationResult ValidateOrientation(VideoProbeResult probe)
    {
        if (probe.Width >= probe.Height)
            return ValidationResult.Success();

        return ValidationResult.Fail(
            "Vertical video is not supported. Please use a landscape orientation file.");
    }

    /// <summary>
    /// Checks that the video uses a constant frame rate (CFR), not variable (VFR).
    /// Compares avg_frame_rate and r_frame_rate within a small tolerance.
    /// </summary>
    /// <returns></returns>
    private static ValidationResult ValidateFrameRate(VideoProbeResult probe)
    {
        if (Math.Abs(probe.FrameRate - probe.RFrameRate) < VfrTolerance)
            return ValidationResult.Success();

        return ValidationResult.Fail(
            "Variable frame rate (VFR) files are not supported. " +
            "Please convert to constant frame rate (CFR) before importing.");
    }
}
