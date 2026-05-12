using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HVSCaptureOne.Core.Models;
using HVSCaptureOne.Core.Services;

namespace HVSCaptureOne.App.ViewModels;

/// <summary>
/// ViewModel for the Edit Project page.
/// Allows editing of client and metadata fields, then optionally reprocessing the video.
/// </summary>
public partial class EditProjectViewModel : ObservableObject
{
    private readonly MainViewModel      _main;
    private readonly Project            _project;
    private readonly VideoAsset         _asset;
    private readonly ProjectService     _projectService     = new();
    private readonly ProcessingService  _processingService  = new();
    private readonly UserProfileService _userProfileService = new();

    /// <summary>
    /// Initializes the edit page with the project and asset to edit.
    /// </summary>
    /// <returns></returns>
    public EditProjectViewModel(MainViewModel main, Project project, VideoAsset asset)
    {
        _main    = main;
        _project = project;
        _asset   = asset;

        ClientName     = project.ClientName;
        ClientEmail    = project.ClientEmail;
        MainTitle      = asset.Metadata.MainTitle;
        SubTitle       = asset.Metadata.SubTitle;
        Description    = asset.Metadata.Description;
        OriginalTapeId = asset.Metadata.OriginalTapeId;
        Notes          = asset.Metadata.Notes;
    }

    // ── Read-only display ────────────────────────────────────────────────────

    /// <summary>Project ID (read-only).</summary>
    public string ProjectId    => _project.ProjectId;

    /// <summary>Project name (read-only).</summary>
    public string ProjectName  => _project.ProjectName;

    /// <summary>Date the project was created (read-only).</summary>
    public string CreatedDate  => _project.CreatedDate.ToString("MM/dd/yyyy");

    /// <summary>Output folder path (read-only).</summary>
    public string OutputFolder => _project.OutputFolder;

    /// <summary>Original source file path (read-only).</summary>
    public string FilePath     => _asset.OriginalFilePath;

    /// <summary>Output file name only (read-only).</summary>
    public string OutputFile   => Path.GetFileName(_asset.OutputFilePath);

    /// <summary>Detected video resolution (read-only).</summary>
    public string Resolution   => _asset.Metadata.DetectedResolution;

    /// <summary>Detected codec (read-only).</summary>
    public string Codec        => _asset.Metadata.DetectedCodec.ToUpper();

    /// <summary>Detected duration formatted for display (read-only).</summary>
    public string Duration     => $"{(int)_asset.Metadata.DetectedDuration.TotalMinutes}m {_asset.Metadata.DetectedDuration.Seconds}s";

    // ── Editable fields ──────────────────────────────────────────────────────

    /// <summary>Client full name.</summary>
    [ObservableProperty] private string _clientName     = string.Empty;

    /// <summary>Client email address.</summary>
    [ObservableProperty] private string _clientEmail    = string.Empty;

    /// <summary>Main video title.</summary>
    [ObservableProperty] private string _mainTitle      = string.Empty;

    /// <summary>Video sub title.</summary>
    [ObservableProperty] private string _subTitle       = string.Empty;

    /// <summary>Video description.</summary>
    [ObservableProperty] private string _description    = string.Empty;

    /// <summary>Original tape ID.</summary>
    [ObservableProperty] private string _originalTapeId = string.Empty;

    /// <summary>Internal studio notes — not embedded in the MP4.</summary>
    [ObservableProperty] private string _notes          = string.Empty;

    // ── Processing state ─────────────────────────────────────────────────────

    /// <summary>True while the reprocess pipeline is running.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanReprocess))]
    [NotifyPropertyChangedFor(nameof(ReprocessButtonText))]
    private bool _isProcessing;

    /// <summary>Status message shown during and after reprocessing.</summary>
    [ObservableProperty] private string _processingStatus = string.Empty;

    /// <summary>True when reprocessing completed successfully.</summary>
    [ObservableProperty] private bool _isComplete;

    /// <summary>True when reprocessing failed.</summary>
    [ObservableProperty] private bool _hasError;

    /// <summary>False while processing — disables the Reprocess button.</summary>
    public bool CanReprocess => !IsProcessing;

    /// <summary>Label on the Reprocess button.</summary>
    public string ReprocessButtonText => IsProcessing ? "Processing…" : "↺  Reprocess";

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Saves any edits to the project JSON and navigates back to the Projects Grid.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void Cancel() => _main.NavigateTo(new ProjectsGridViewModel(_main));

    /// <summary>
    /// Commits edits, then re-runs the full processing pipeline on the original source file.
    /// Navigates back to the Projects Grid on success after a short countdown.
    /// </summary>
    /// <returns></returns>
    [RelayCommand(CanExecute = nameof(CanReprocess))]
    private async Task ReprocessAsync()
    {
        CommitEdits();

        if (!File.Exists(_asset.OriginalFilePath))
        {
            HasError         = true;
            ProcessingStatus = $"Source file not found:\n{_asset.OriginalFilePath}";
            return;
        }

        IsProcessing = true;
        IsComplete   = false;
        HasError     = false;
        ReprocessCommand.NotifyCanExecuteChanged();

        var profile  = _userProfileService.Load() ?? new UserProfile { HVSLocationNumber = "55" };
        var progress = new Progress<string>(msg => ProcessingStatus = msg);

        var result = await Task.Run(() =>
            _processingService.Process(
                _asset.OriginalFilePath,
                _asset.OutputFilePath,
                _project,
                _asset,
                profile,
                progress));

        IsProcessing = false;
        ReprocessCommand.NotifyCanExecuteChanged();

        if (result.Success)
        {
            IsComplete = true;
            string chapterNote = result.ChapterCount > 0
                ? $" — {result.ChapterCount} chapters embedded"
                : string.Empty;

            for (int i = 5; i > 0; i--)
            {
                ProcessingStatus = $"Processing complete{chapterNote}. Returning to grid in {i}…";
                await Task.Delay(1000);
            }

            _main.NavigateTo(new ProjectsGridViewModel(_main));
        }
        else
        {
            HasError         = true;
            ProcessingStatus = result.ErrorMessage;
        }
    }

    /// <summary>
    /// Writes all editable field values back to the domain model and saves to disk.
    /// </summary>
    /// <returns></returns>
    private void CommitEdits()
    {
        _project.ClientName  = ClientName.Trim();
        _project.ClientEmail = ClientEmail.Trim();

        _asset.Metadata.ClientName     = ClientName.Trim();
        _asset.Metadata.ClientEmail    = ClientEmail.Trim();
        _asset.Metadata.MainTitle      = MainTitle.Trim();
        _asset.Metadata.SubTitle       = SubTitle.Trim();
        _asset.Metadata.Description    = Description.Trim();
        _asset.Metadata.OriginalTapeId = OriginalTapeId.Trim();
        _asset.Metadata.Notes          = Notes.Trim();

        _projectService.Save(_project);
    }
}
