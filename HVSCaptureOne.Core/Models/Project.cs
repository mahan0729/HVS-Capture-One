namespace HVSCaptureOne.Core.Models;

public class Project
{
    // User-assigned project ID — used as the tpnm atom value (e.g. "pasotti_sandy01")
    public string ProjectId { get; set; } = string.Empty;

    public string ProjectName { get; set; } = string.Empty;

    // Client info — shared across all assets in the project
    public string ClientName { get; set; } = string.Empty;
    public string ClientEmail { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    // User selects this once per project; becomes the default for subsequent assets
    public string OutputFolder { get; set; } = string.Empty;

    public List<VideoAsset> Assets { get; set; } = new();
}
