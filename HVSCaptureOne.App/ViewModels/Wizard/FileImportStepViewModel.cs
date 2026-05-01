using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HVSCaptureOne.Core.Services;
using Microsoft.Win32;

namespace HVSCaptureOne.App.ViewModels.Wizard;

/// <summary>
/// ViewModel for Step 2 of the New Project wizard: file import and validation.
/// Runs FFprobe and ValidationService on the selected MP4 file.
/// </summary>
public partial class FileImportStepViewModel : ObservableObject
{
    private readonly ProjectWizardViewModel _wizard;

    /// <summary>
    /// Initializes Step 2 with a reference to the parent wizard.
    /// </summary>
    /// <returns></returns>
    public FileImportStepViewModel(ProjectWizardViewModel wizard)
    {
        _wizard = wizard;
    }

    /// <summary>Gets or sets the full path of the selected source MP4 file.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(HasFile))]
    private string _filePath = string.Empty;

    /// <summary>Gets or sets the detected resolution string (e.g. "1440x1080").</summary>
    [ObservableProperty] private string _detectedResolution = string.Empty;

    /// <summary>Gets or sets the detected codec name (e.g. "h264").</summary>
    [ObservableProperty] private string _detectedCodec = string.Empty;

    /// <summary>Gets or sets the detected duration formatted for display.</summary>
    [ObservableProperty] private string _detectedDuration = string.Empty;

    /// <summary>Gets or sets the validation status message shown to the user.</summary>
    [ObservableProperty] private string _statusMessage = string.Empty;

    /// <summary>Gets or sets whether the status message represents an error.</summary>
    [ObservableProperty] private bool _isError;

    /// <summary>Gets or sets whether the status message represents success.</summary>
    [ObservableProperty] private bool _isSuccess;

    /// <summary>True when a valid file has been accepted and the step can proceed.</summary>
    public bool IsValid => IsSuccess && !string.IsNullOrEmpty(FilePath);

    /// <summary>True when any file has been selected (valid or not).</summary>
    public bool HasFile => !string.IsNullOrEmpty(FilePath);

    /// <summary>
    /// The raw FFprobe result for the accepted file.
    /// Null until a valid file has been accepted.
    /// Consumed by ReviewStepViewModel to populate VideoMetadata.
    /// </summary>
    public HVSCaptureOne.Core.Models.VideoProbeResult? ProbeResult { get; private set; }

    /// <summary>
    /// Opens a file browser dialog filtered to MP4 files.
    /// Runs FFprobe and validation on the selected file.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Title  = "Select MP4 File",
            Filter = "MP4 Files (*.mp4)|*.mp4"
        };

        if (dialog.ShowDialog() == true)
            ProcessFile(dialog.FileName);
    }

    /// <summary>
    /// Handles a file path dropped onto the drag-drop zone.
    /// Runs FFprobe and validation on the dropped file.
    /// </summary>
    /// <returns></returns>
    public void HandleDrop(string droppedFilePath)
    {
        ProcessFile(droppedFilePath);
    }

    /// <summary>
    /// Runs FFprobe and ValidationService on the given file path.
    /// Updates status properties based on the result.
    /// </summary>
    /// <returns></returns>
    private void ProcessFile(string path)
    {
        // Reset state
        FilePath          = string.Empty;
        DetectedResolution = string.Empty;
        DetectedCodec     = string.Empty;
        DetectedDuration  = string.Empty;
        StatusMessage     = string.Empty;
        IsError           = false;
        IsSuccess         = false;

        try
        {
            var ffprobe    = new FFprobeService(App.FFprobePath);
            var validation = new ValidationService();

            var probe  = ffprobe.Probe(path);
            var result = validation.Validate(probe);

            if (!result.IsValid)
            {
                StatusMessage = result.ErrorMessage;
                IsError       = true;
                _wizard.RefreshNavigation();
                return;
            }

            FilePath           = path;
            ProbeResult        = probe;
            DetectedResolution = $"{probe.Width}x{probe.Height}";
            DetectedCodec      = probe.CodecName.ToUpper();
            DetectedDuration   = $"{(int)probe.Duration.TotalMinutes}m {probe.Duration.Seconds}s";
            StatusMessage      = "File accepted.";
            IsSuccess          = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not read file: {ex.Message}";
            IsError       = true;
        }

        _wizard.RefreshNavigation();
    }
}
