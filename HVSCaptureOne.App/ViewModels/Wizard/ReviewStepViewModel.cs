using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HVSCaptureOne.App.ViewModels;
using HVSCaptureOne.Core.Models;
using HVSCaptureOne.Core.Services;

namespace HVSCaptureOne.App.ViewModels.Wizard;

/// <summary>
/// ViewModel for Step 5 of the New Project wizard: review and process.
/// Displays a summary of all entered data, then runs the full processing pipeline.
/// </summary>
public partial class ReviewStepViewModel : ObservableObject
{
    private readonly ProjectWizardViewModel _wizard;
    private readonly MainViewModel          _main;
    private readonly ProcessingService      _processingService = new();
    private readonly UserProfileService     _profileService    = new();

    /// <summary>
    /// Initializes the Review step with references to the wizard and main navigation.
    /// </summary>
    /// <returns></returns>
    public ReviewStepViewModel(ProjectWizardViewModel wizard, MainViewModel main)
    {
        _wizard = wizard;
        _main   = main;
    }

    // ── Summary properties (read-only, sourced from prior steps) ─────────────

    /// <summary>Summary: project name from Step 1.</summary>
    public string ProjectName    => _wizard.ProjectInfo.ProjectName;

    /// <summary>Summary: client full name from Step 1.</summary>
    public string ClientName     => $"{_wizard.ProjectInfo.ClientFirstName} {_wizard.ProjectInfo.ClientLastName}".Trim();

    /// <summary>Summary: client email from Step 1.</summary>
    public string ClientEmail    => _wizard.ProjectInfo.ClientEmail;

    /// <summary>Summary: output folder from Step 1.</summary>
    public string OutputFolder   => _wizard.ProjectInfo.OutputFolder;

    /// <summary>Summary: selected file path from Step 2.</summary>
    public string FilePath       => _wizard.FileImport.FilePath;

    /// <summary>Summary: detected resolution from Step 2.</summary>
    public string Resolution     => _wizard.FileImport.DetectedResolution;

    /// <summary>Summary: detected codec from Step 2.</summary>
    public string Codec          => _wizard.FileImport.DetectedCodec;

    /// <summary>Summary: detected duration from Step 2.</summary>
    public string Duration       => _wizard.FileImport.DetectedDuration;

    /// <summary>Summary: effective output filename from Step 3.</summary>
    public string OutputFileName => _wizard.FileNaming.EffectiveFileName;

    /// <summary>Summary: main title from Step 4.</summary>
    public string MainTitle      => _wizard.MetadataForm.MainTitle;

    /// <summary>Summary: sub title from Step 4.</summary>
    public string SubTitle       => _wizard.MetadataForm.SubTitle;

    /// <summary>Summary: description from Step 4.</summary>
    public string Description    => _wizard.MetadataForm.Description;

    /// <summary>Summary: original tape ID from Step 4.</summary>
    public string OriginalTapeId => _wizard.MetadataForm.OriginalTapeId;

    // ── Processing state ─────────────────────────────────────────────────────

    /// <summary>True while the processing pipeline is running.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanProcess))]
    [NotifyPropertyChangedFor(nameof(ProcessButtonText))]
    private bool _isProcessing;

    /// <summary>Status message shown during and after processing.</summary>
    [ObservableProperty] private string _processingStatus = string.Empty;

    /// <summary>True when processing completed successfully.</summary>
    [ObservableProperty] private bool _isComplete;

    /// <summary>True when processing failed.</summary>
    [ObservableProperty] private bool _hasError;

    /// <summary>Full path to the written output file, shown on success.</summary>
    [ObservableProperty] private string _outputPath = string.Empty;

    /// <summary>False while processing is running — disables the Process button.</summary>
    public bool CanProcess => !IsProcessing;

    /// <summary>Label shown on the Process button — changes while processing is running.</summary>
    public string ProcessButtonText => IsProcessing ? "Video Processing..." : "Process Video";

    // ── Process command ───────────────────────────────────────────────────────

    /// <summary>
    /// Runs the full DVA processing pipeline on a background thread.
    /// Builds the Project and VideoAsset from all wizard steps, calls
    /// ProcessingService, and updates status properties for the UI.
    /// </summary>
    /// <returns></returns>
    [RelayCommand(CanExecute = nameof(CanProcess))]
    private async Task ProcessVideoAsync()
    {
        IsProcessing    = true;
        IsComplete      = false;
        HasError        = false;
        ProcessingStatus = "Starting…";

        ProcessVideoCommand.NotifyCanExecuteChanged();

        var project    = BuildProject();
        var asset      = BuildAsset(project);
        var outputPath = System.IO.Path.Combine(
            project.OutputFolder,
            $"{OutputFileName}.mp4");

        var profile  = _profileService.Load() ?? new UserProfile { HVSLocationNumber = "55" };
        var progress = new Progress<string>(msg => ProcessingStatus = msg);

        var result = await Task.Run(() =>
            _processingService.Process(
                _wizard.FileImport.FilePath,
                outputPath,
                project,
                asset,
                profile,
                progress));

        IsProcessing = false;
        ProcessVideoCommand.NotifyCanExecuteChanged();

        if (result.Success)
        {
            IsComplete       = true;
            OutputPath       = result.OutputPath;

            for (int i = 10; i > 0; i--)
            {
                ProcessingStatus = $"Processing complete. Returning to main screen in {i}…";
                await Task.Delay(1000);
            }

            _main.NavigateTo(new DashboardViewModel(_main));
        }
        else
        {
            HasError         = true;
            ProcessingStatus = result.ErrorMessage;
        }
    }

    // ── Domain object builders ────────────────────────────────────────────────

    /// <summary>
    /// Builds a Project from the Step 1 data.
    /// </summary>
    /// <returns>Populated Project ready for ProcessingService.</returns>
    private Project BuildProject()
    {
        var info = _wizard.ProjectInfo;
        return new Project
        {
            ProjectId    = info.GeneratedProjectId,
            ProjectName  = info.ProjectName,
            ClientName   = $"{info.ClientFirstName} {info.ClientLastName}".Trim(),
            ClientEmail  = info.ClientEmail,
            CreatedDate  = DateTime.Now,
            OutputFolder = info.OutputFolder,
        };
    }

    /// <summary>
    /// Builds a VideoAsset from Steps 2–4 data, including all metadata fields.
    /// </summary>
    /// <returns>Populated VideoAsset ready for ProcessingService.</returns>
    private VideoAsset BuildAsset(Project project)
    {
        var import = _wizard.FileImport;
        var meta   = _wizard.MetadataForm;
        var naming = _wizard.FileNaming;

        var duration = import.ProbeResult?.Duration ?? TimeSpan.Zero;

        return new VideoAsset
        {
            AssetId          = Guid.NewGuid(),
            OriginalFilePath = import.FilePath,
            SequenceNumber   = 1,
            FileNameOverride = naming.UseSuggestedName ? string.Empty : naming.CustomFileName,
            Status           = AssetStatus.Pending,
            Metadata = new VideoMetadata
            {
                MainTitle          = meta.MainTitle,
                SubTitle           = meta.SubTitle,
                Description        = meta.Description,
                OriginalTapeId     = meta.OriginalTapeId,
                Notes              = meta.Notes,
                ClientName         = project.ClientName,
                ClientEmail        = project.ClientEmail,
                DetectedDuration   = duration,
                DetectedResolution = import.DetectedResolution,
                DetectedCodec      = import.DetectedCodec,
            }
        };
    }
}
