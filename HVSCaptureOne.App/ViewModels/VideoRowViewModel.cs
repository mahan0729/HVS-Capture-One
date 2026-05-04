using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HVSCaptureOne.Core.Models;
using HVSCaptureOne.Core.Services;

namespace HVSCaptureOne.App.ViewModels;

/// <summary>
/// Represents a single video row in the Projects grid.
/// Supports inline editing of client and metadata fields.
/// </summary>
public partial class VideoRowViewModel : ObservableObject
{
    private readonly Project    _project;
    private readonly VideoAsset _asset;
    private readonly ProjectService              _projectService;
    private readonly Action<VideoRowViewModel>   _onDeleted;

    private string _origClientName  = string.Empty;
    private string _origClientEmail = string.Empty;
    private string _origMainTitle   = string.Empty;
    private string _origSubTitle    = string.Empty;
    private string _origDescription = string.Empty;

    /// <summary>
    /// Initializes a row bound to the given project and asset.
    /// </summary>
    /// <returns></returns>
    public VideoRowViewModel(
        Project                    project,
        VideoAsset                 asset,
        ProjectService             projectService,
        Action<VideoRowViewModel>  onDeleted)
    {
        _project        = project;
        _asset          = asset;
        _projectService = projectService;
        _onDeleted      = onDeleted;

        ClientName  = project.ClientName;
        ClientEmail = project.ClientEmail;
        MainTitle   = asset.Metadata.MainTitle;
        SubTitle    = asset.Metadata.SubTitle;
        Description = asset.Metadata.Description;
    }

    // ── Read-only display ────────────────────────────────────────────────────

    /// <summary>Project ID (e.g. "hanley_robert_01").</summary>
    public string ProjectId  => _project.ProjectId;

    /// <summary>Date the project was created.</summary>
    public string Date       => _project.CreatedDate.ToString("MM/dd/yyyy");

    /// <summary>Processing status of the asset.</summary>
    public string Status     => _asset.Status.ToString();

    /// <summary>Output filename without directory path.</summary>
    public string OutputFile => Path.GetFileName(_asset.OutputFilePath);

    // ── Edit state ───────────────────────────────────────────────────────────

    /// <summary>True while the row is in edit mode.</summary>
    [ObservableProperty] private bool _isEditing;

    // ── Editable fields ──────────────────────────────────────────────────────

    /// <summary>Client full name.</summary>
    [ObservableProperty] private string _clientName  = string.Empty;

    /// <summary>Client email address.</summary>
    [ObservableProperty] private string _clientEmail = string.Empty;

    /// <summary>Main video title.</summary>
    [ObservableProperty] private string _mainTitle   = string.Empty;

    /// <summary>Video sub title.</summary>
    [ObservableProperty] private string _subTitle    = string.Empty;

    /// <summary>Video description.</summary>
    [ObservableProperty] private string _description = string.Empty;

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Enters edit mode, snapshotting current values so Cancel can revert them.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void Edit()
    {
        _origClientName  = ClientName;
        _origClientEmail = ClientEmail;
        _origMainTitle   = MainTitle;
        _origSubTitle    = SubTitle;
        _origDescription = Description;
        IsEditing = true;
    }

    /// <summary>
    /// Commits edits to the project model and saves to disk.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void SaveEdit()
    {
        _project.ClientName  = ClientName.Trim();
        _project.ClientEmail = ClientEmail.Trim();

        _asset.Metadata.ClientName   = ClientName.Trim();
        _asset.Metadata.ClientEmail  = ClientEmail.Trim();
        _asset.Metadata.MainTitle    = MainTitle.Trim();
        _asset.Metadata.SubTitle     = SubTitle.Trim();
        _asset.Metadata.Description  = Description.Trim();

        // Sync display values to trimmed result
        ClientName  = _project.ClientName;
        ClientEmail = _project.ClientEmail;
        MainTitle   = _asset.Metadata.MainTitle;
        SubTitle    = _asset.Metadata.SubTitle;
        Description = _asset.Metadata.Description;

        _projectService.Save(_project);
        IsEditing = false;
    }

    /// <summary>
    /// Cancels edits, restoring the values captured when Edit was clicked.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void CancelEdit()
    {
        ClientName  = _origClientName;
        ClientEmail = _origClientEmail;
        MainTitle   = _origMainTitle;
        SubTitle    = _origSubTitle;
        Description = _origDescription;
        IsEditing   = false;
    }

    /// <summary>
    /// Prompts for confirmation, then removes this asset from its project.
    /// Deletes the project file entirely if no assets remain.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void Delete()
    {
        var answer = MessageBox.Show(
            $"Delete the record for \"{MainTitle}\"?\n\nThis removes the project record only — the output file on disk is not deleted.",
            "Delete Video Record",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (answer != MessageBoxResult.Yes)
            return;

        _project.Assets.Remove(_asset);

        if (_project.Assets.Count == 0)
            _projectService.Delete(_project.ProjectId);
        else
            _projectService.Save(_project);

        _onDeleted(this);
    }
}
