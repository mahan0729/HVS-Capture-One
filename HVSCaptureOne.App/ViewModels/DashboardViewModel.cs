using CommunityToolkit.Mvvm.Input;
using HVSCaptureOne.App.ViewModels.Wizard;

namespace HVSCaptureOne.App.ViewModels;

/// <summary>
/// ViewModel for the main dashboard screen.
/// Entry point after profile setup or on subsequent launches.
/// </summary>
public partial class DashboardViewModel
{
    private readonly MainViewModel _main;

    /// <summary>
    /// Initializes the Dashboard ViewModel.
    /// </summary>
    /// <returns></returns>
    public DashboardViewModel(MainViewModel main)
    {
        _main = main;
    }

    /// <summary>
    /// Navigates to the New Project wizard.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void NewProject()
    {
        _main.NavigateTo(new ProjectWizardViewModel(_main));
    }
}
