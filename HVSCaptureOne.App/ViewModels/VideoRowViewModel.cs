using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HVSCaptureOne.Core.Models;
using HVSCaptureOne.Core.Services;

namespace HVSCaptureOne.App.ViewModels;

/// <summary>
/// Represents a single video row in the Projects grid.
/// </summary>
public partial class VideoRowViewModel : ObservableObject
{
    private readonly MainViewModel              _main;
    private readonly Project                    _project;
    private readonly VideoAsset                 _asset;
    private readonly ProjectService             _projectService;
    private readonly Action<VideoRowViewModel>  _onDeleted;

    /// <summary>
    /// Initializes a row bound to the given project and asset.
    /// </summary>
    /// <returns></returns>
    public VideoRowViewModel(
        MainViewModel              main,
        Project                    project,
        VideoAsset                 asset,
        ProjectService             projectService,
        Action<VideoRowViewModel>  onDeleted)
    {
        _main           = main;
        _project        = project;
        _asset          = asset;
        _projectService = projectService;
        _onDeleted      = onDeleted;
    }

    // ── Display properties ───────────────────────────────────────────────────

    /// <summary>Project ID (e.g. "hanley_robert_01").</summary>
    public string ProjectId  => _project.ProjectId;

    /// <summary>Client full name.</summary>
    public string ClientName  => _project.ClientName;

    /// <summary>Client email address.</summary>
    public string ClientEmail => _project.ClientEmail;

    /// <summary>Main video title.</summary>
    public string MainTitle   => _asset.Metadata.MainTitle;

    /// <summary>Video sub title.</summary>
    public string SubTitle    => _asset.Metadata.SubTitle;

    /// <summary>Video description.</summary>
    public string Description => _asset.Metadata.Description;

    /// <summary>Date the project was created.</summary>
    public string Date        => _project.CreatedDate.ToString("MM/dd/yyyy");

    /// <summary>Processing status of the asset.</summary>
    public string Status      => _asset.Status.ToString();

    /// <summary>Output filename without directory path.</summary>
    public string OutputFile  => Path.GetFileName(_asset.OutputFilePath);

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Navigates to the Edit Project page for this row.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void Edit() =>
        _main.NavigateTo(new EditProjectViewModel(_main, _project, _asset));

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
