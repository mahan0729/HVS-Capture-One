using System.IO;
using System.Windows;
using HVSCaptureOne.App.ViewModels;
using HVSCaptureOne.Core.Services;

namespace HVSCaptureOne.App;

public partial class App : Application
{
    /// <summary>
    /// True if no user profile exists on disk — triggers the first-launch setup wizard.
    /// </summary>
    public static bool IsFirstLaunch { get; private set; }

    /// <summary>
    /// Full path to the bundled ffprobe.exe, resolved from the application directory.
    /// Used by FFprobeService throughout the workflow.
    /// </summary>
    public static string FFprobePath { get; private set; } = string.Empty;

    /// <summary>
    /// Runs on application startup. Detects first launch, resolves FFprobe path,
    /// creates the MainViewModel, and shows the MainWindow.
    /// </summary>
    /// <returns></returns>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var profileService = new UserProfileService();
        IsFirstLaunch = !profileService.Exists();

        FFprobePath = Path.Combine(AppContext.BaseDirectory, "ffprobe.exe");

        var mainVm = new MainViewModel();

        if (IsFirstLaunch)
            mainVm.NavigateTo(new UserProfileSetupViewModel(mainVm, profileService));
        else
            mainVm.NavigateTo(new DashboardViewModel(mainVm));

        var window = new MainWindow { DataContext = mainVm };
        window.Show();
    }
}
