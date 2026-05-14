using HVSCaptureOne.Core.Models;
using Serilog;

namespace HVSCaptureOne.Core.Services;

/// <summary>
/// Orchestrates the full video processing pipeline for a single asset:
/// scene detection, chapter generation, thumbnail extraction, atom building,
/// and output MP4 writing. The source file is never modified.
/// </summary>
public class ProcessingService
{
    private readonly string         _ffmpegPath;
    private readonly DvaAtomBuilder _atomBuilder = new();
    private readonly Mp4BoxWriter   _writer      = new();

    /// <summary>
    /// Initializes ProcessingService with the full path to the ffmpeg executable.
    /// </summary>
    /// <returns></returns>
    public ProcessingService(string ffmpegPath)
    {
        _ffmpegPath = ffmpegPath;
    }

    /// <summary>
    /// Processes one video asset synchronously.
    /// Intended to be called from a background thread via Task.Run.
    /// </summary>
    /// <param name="sourcePath">Full path to the source MP4 (never modified).</param>
    /// <param name="outputPath">Full path for the output MP4 (created or overwritten).</param>
    /// <param name="project">Project containing client info and project ID.</param>
    /// <param name="asset">Asset containing metadata and detected video properties.</param>
    /// <param name="profile">Operator profile supplying location number and scene sensitivity.</param>
    /// <param name="progress">Optional progress reporter for UI feedback.</param>
    /// <returns>ProcessingResult indicating success or failure with an error message.</returns>
    public ProcessingResult Process(
        string             sourcePath,
        string             outputPath,
        Project            project,
        VideoAsset         asset,
        UserProfile        profile,
        IProgress<string>? progress = null)
    {
        Log.Information(
            "Processing started. Project={ProjectId} Source={Source} Output={Output}",
            project.ProjectId, sourcePath, outputPath);

        try
        {
            // Pre-flight: verify source still exists
            if (!File.Exists(sourcePath))
                return Fail(asset, $"Source file no longer exists: {sourcePath}");

            // Ensure output directory exists
            string? outputDir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            // Verify output folder is writable with a quick probe
            VerifyOutputFolderWritable(outputDir ?? outputPath);

            // Scene detection — produces ordered list of chapter timestamps
            progress?.Report("Detecting scenes…");
            var sceneService = new SceneDetectionService(_ffmpegPath);
            var timestamps   = sceneService.Detect(
                sourcePath,
                profile.SceneDetectionSensitivity,
                asset.Metadata.DetectedDuration);

            asset.Metadata.Chapters = ChapterService.FromTimestamps(timestamps);
            Log.Debug("Chapters generated. Count={Count} Sensitivity={Sensitivity}",
                asset.Metadata.Chapters.Count, profile.SceneDetectionSensitivity);

            // Thumbnail extraction — one 1280×720 JPG per chapter point
            progress?.Report("Extracting thumbnails…");
            string thumbDir  = Path.Combine(outputDir ?? outputPath, "thumbnails");
            string thumbBase = Path.GetFileNameWithoutExtension(outputPath);
            var    thumbService = new ThumbnailService(_ffmpegPath);
            asset.Metadata.ThumbnailPaths = thumbService.Extract(
                sourcePath, thumbDir, thumbBase, timestamps);

            progress?.Report("Building metadata atoms…");
            Log.Debug("Building DVA atom list for project {ProjectId}", project.ProjectId);
            var atoms = _atomBuilder.Build(project, asset, profile, DateTime.Now);
            Log.Debug("Built {Count} atoms", atoms.Count);

            progress?.Report("Writing output file…");
            Log.Debug("Writing output MP4: {Output}", outputPath);
            _writer.Write(sourcePath, outputPath, atoms);

            asset.OutputFilePath = outputPath;
            asset.Status         = AssetStatus.Complete;

            var fileInfo = new FileInfo(outputPath);
            Log.Information(
                "Processing complete. Output={Output} Chapters={Chapters} Thumbnails={Thumbs} Size={Size:N0} bytes",
                outputPath, asset.Metadata.Chapters.Count, asset.Metadata.ThumbnailPaths.Count, fileInfo.Length);

            return ProcessingResult.Ok(outputPath, asset.Metadata.Chapters.Count);
        }
        catch (UnauthorizedAccessException ex)
        {
            const string msg = "Access denied to the output folder. Check folder permissions.";
            Log.Error(ex, msg);
            return Fail(asset, msg);
        }
        catch (IOException ex)
        {
            string msg = $"File I/O error: {ex.Message}";
            Log.Error(ex, "File I/O error during processing");
            return Fail(asset, msg);
        }
        catch (InvalidOperationException ex)
        {
            Log.Error(ex, "Processing error: {Message}", ex.Message);
            return Fail(asset, ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error during processing");
            return Fail(asset, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Marks the asset as failed and returns a failed ProcessingResult.
    /// </summary>
    /// <returns></returns>
    private static ProcessingResult Fail(VideoAsset asset, string message)
    {
        asset.Status = AssetStatus.Failed;
        Log.Warning("Processing failed: {Message}", message);
        return ProcessingResult.Fail(message);
    }

    /// <summary>
    /// Writes and immediately deletes a small probe file to confirm the output
    /// folder is writable before beginning a long processing operation.
    /// Throws UnauthorizedAccessException if the folder is not writable.
    /// </summary>
    /// <returns></returns>
    private static void VerifyOutputFolderWritable(string folderPath)
    {
        string probe = Path.Combine(folderPath, $".hvs_write_check_{Guid.NewGuid():N}");
        File.WriteAllText(probe, string.Empty);
        File.Delete(probe);
    }
}
