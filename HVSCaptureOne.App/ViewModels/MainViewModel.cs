using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HVSCaptureOne.App.Views;

namespace HVSCaptureOne.App.ViewModels;

/// <summary>
/// Root ViewModel for MainWindow. Owns navigation by swapping CurrentView,
/// which is bound to the ContentControl in MainWindow.xaml.
/// DataTemplates in App.xaml map each ViewModel type to its corresponding View.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    /// <summary>
    /// The ViewModel currently displayed in the main content area.
    /// Changing this property swaps the visible view automatically via DataTemplates.
    /// </summary>
    [ObservableProperty]
    private object _currentView = new object();

    /// <summary>
    /// Navigates to the given ViewModel, which causes its paired View to render.
    /// </summary>
    /// <returns></returns>
    public void NavigateTo(object viewModel) => CurrentView = viewModel;

    /// <summary>
    /// Opens the Help modal dialog. Available on every screen via the master banner.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void ShowHelp() => new HelpDialog().ShowDialog();
}
