namespace HVSCaptureOne.Core.Models;

public class VideoAsset
{
    public Guid AssetId { get; set; } = Guid.NewGuid();

    // Source file — read-only, never modified by the software
    public string OriginalFilePath { get; set; } = string.Empty;

    // Paths used during and after processing
    public string WorkingCopyPath { get; set; } = string.Empty;
    public string OutputFilePath { get; set; } = string.Empty;

    // File naming — sequence number within the project (01, 02, 03...)
    public int SequenceNumber { get; set; }

    // Populated if the user chose to override the auto-generated filename
    public string FileNameOverride { get; set; } = string.Empty;

    public VideoMetadata Metadata { get; set; } = new();
    public AssetStatus Status { get; set; } = AssetStatus.Pending;
}
