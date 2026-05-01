using HVSCaptureOne.Core.Models;

namespace HVSCaptureOne.Core.Services;

/// <summary>
/// Orchestrates the full video processing pipeline for a single asset:
/// builds the DVA atom list, then writes the output MP4.
/// The source file is never modified.
/// </summary>
public class ProcessingService
{
    private readonly DvaAtomBuilder _atomBuilder = new();
    private readonly Mp4BoxWriter   _writer      = new();

    /// <summary>
    /// Processes one video asset synchronously.
    /// Intended to be called from a background thread via Task.Run.
    /// </summary>
    /// <param name="sourcePath">Full path to the source MP4 (never modified).</param>
    /// <param name="outputPath">Full path for the output MP4 (created or overwritten).</param>
    /// <param name="project">Project containing client info and project ID.</param>
    /// <param name="asset">Asset containing metadata and detected video properties.</param>
    /// <param name="profile">Operator profile supplying the HVS location number.</param>
    /// <param name="progress">Optional progress reporter for UI feedback.</param>
    /// <returns>ProcessingResult indicating success or failure with an error message.</returns>
    public ProcessingResult Process(
        string          sourcePath,
        string          outputPath,
        Project         project,
        VideoAsset      asset,
        UserProfile     profile,
        IProgress<string>? progress = null)
    {
        try
        {
            // Ensure the output directory exists
            string? outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            progress?.Report("Building metadata atoms…");
            var atoms = _atomBuilder.Build(project, asset, profile, DateTime.Now);

            progress?.Report("Writing output file…");
            _writer.Write(sourcePath, outputPath, atoms);

            asset.OutputFilePath = outputPath;
            asset.Status         = AssetStatus.Complete;

            return ProcessingResult.Ok(outputPath);
        }
        catch (Exception ex)
        {
            asset.Status = AssetStatus.Failed;
            return ProcessingResult.Fail(ex.Message);
        }
    }
}
