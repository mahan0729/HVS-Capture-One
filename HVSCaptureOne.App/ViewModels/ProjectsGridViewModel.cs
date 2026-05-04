using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HVSCaptureOne.App.ViewModels.Wizard;
using HVSCaptureOne.Core.Services;

namespace HVSCaptureOne.App.ViewModels;

/// <summary>
/// ViewModel for the Projects grid screen.
/// Loads all saved projects and exposes one VideoRowViewModel per video asset.
/// </summary>
public partial class ProjectsGridViewModel : ObservableObject
{
    private readonly MainViewModel  _main;
    private readonly ProjectService _projectService = new();

    /// <summary>All video rows — one per asset across all saved projects.</summary>
    public ObservableCollection<VideoRowViewModel> Rows { get; } = new();

    /// <summary>True when at least one row exists; drives grid vs. empty-state visibility.</summary>
    public bool HasRows => Rows.Count > 0;

    /// <summary>
    /// Initializes the grid and loads all saved projects from disk.
    /// </summary>
    /// <returns></returns>
    public ProjectsGridViewModel(MainViewModel main)
    {
        _main = main;
        LoadRows();
    }

    private void LoadRows()
    {
        Rows.Clear();
        foreach (var project in _projectService.LoadAll())
        {
            foreach (var asset in project.Assets)
                Rows.Add(new VideoRowViewModel(project, asset, _projectService, RemoveRow));
        }
        OnPropertyChanged(nameof(HasRows));
    }

    private void RemoveRow(VideoRowViewModel row)
    {
        Rows.Remove(row);
        OnPropertyChanged(nameof(HasRows));
    }

    /// <summary>Navigates to the New Project wizard.</summary>
    /// <returns></returns>
    [RelayCommand]
    private void NewProject() => _main.NavigateTo(new ProjectWizardViewModel(_main));

    /// <summary>Exits the application.</summary>
    /// <returns></returns>
    [RelayCommand]
    private void Exit() => Application.Current.Shutdown();
}
