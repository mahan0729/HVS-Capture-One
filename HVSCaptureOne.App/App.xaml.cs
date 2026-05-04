using System.IO;
using System.Windows;
using HVSCaptureOne.App.ViewModels;
using HVSCaptureOne.Core.Services;
using Serilog;

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
    /// Runs on application startup. Configures logging, detects first launch,
    /// resolves FFprobe path, creates the MainViewModel, and shows the MainWindow.
    /// </summary>
    /// <returns></returns>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigureLogging();

        var profileService = new UserProfileService();
        IsFirstLaunch = !profileService.Exists();

        FFprobePath = Path.Combine(AppContext.BaseDirectory, "ffprobe.exe");

        Log.Information(
            "HVS Capture One started. FirstLaunch={IsFirstLaunch} FFprobePath={FFprobePath}",
            IsFirstLaunch, FFprobePath);

        var mainVm = new MainViewModel();

        if (IsFirstLaunch)
            mainVm.NavigateTo(new UserProfileSetupViewModel(mainVm, profileService));
        else
            mainVm.NavigateTo(new ProjectsGridViewModel(mainVm));

        var window = new MainWindow { DataContext = mainVm };
        window.Show();
    }

    /// <summary>
    /// Flushes the Serilog logger on application exit.
    /// </summary>
    /// <returns></returns>
    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("HVS Capture One exiting.");
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    /// <summary>
    /// Configures the Serilog static logger with a rolling file sink and a debug sink.
    /// Log files are written to %AppData%\HVSCaptureOne\logs\ and retained for 14 days.
    /// </summary>
    /// <returns></returns>
    private static void ConfigureLogging()
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HVSCaptureOne", "logs");

        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Debug(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logDir, "hvscapture-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
