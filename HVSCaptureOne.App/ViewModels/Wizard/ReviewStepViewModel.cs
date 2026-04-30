using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HVSCaptureOne.App.ViewModels;
using System.Windows;

namespace HVSCaptureOne.App.ViewModels.Wizard;

/// <summary>
/// ViewModel for Step 5 of the New Project wizard: review and process.
/// Displays a summary of all entered data and exposes the Process command.
/// Processing logic is implemented in M5 — this step shows a placeholder for now.
/// </summary>
public partial class ReviewStepViewModel : ObservableObject
{
    private readonly ProjectWizardViewModel _wizard;
    private readonly MainViewModel _main;

    /// <summary>
    /// Initializes the Review step with references to the wizard and main navigation.
    /// </summary>
    /// <returns></returns>
    public ReviewStepViewModel(ProjectWizardViewModel wizard, MainViewModel main)
    {
        _wizard = wizard;
        _main   = main;
    }

    /// <summary>
    /// Summary: project name from Step 1.
    /// </summary>
    public string ProjectName      => _wizard.ProjectInfo.ProjectName;

    /// <summary>
    /// Summary: client full name from Step 1.
    /// </summary>
    public string ClientName       => $"{_wizard.ProjectInfo.ClientFirstName} {_wizard.ProjectInfo.ClientLastName}".Trim();

    /// <summary>
    /// Summary: client email from Step 1.
    /// </summary>
    public string ClientEmail      => _wizard.ProjectInfo.ClientEmail;

    /// <summary>
    /// Summary: output folder from Step 1.
    /// </summary>
    public string OutputFolder     => _wizard.ProjectInfo.OutputFolder;

    /// <summary>
    /// Summary: selected file path from Step 2.
    /// </summary>
    public string FilePath         => _wizard.FileImport.FilePath;

    /// <summary>
    /// Summary: detected resolution from Step 2.
    /// </summary>
    public string Resolution       => _wizard.FileImport.DetectedResolution;

    /// <summary>
    /// Summary: detected codec from Step 2.
    /// </summary>
    public string Codec            => _wizard.FileImport.DetectedCodec;

    /// <summary>
    /// Summary: detected duration from Step 2.
    /// </summary>
    public string Duration         => _wizard.FileImport.DetectedDuration;

    /// <summary>
    /// Summary: effective output filename from Step 3.
    /// </summary>
    public string OutputFileName   => _wizard.FileNaming.EffectiveFileName;

    /// <summary>
    /// Summary: main title from Step 4.
    /// </summary>
    public string MainTitle        => _wizard.MetadataForm.MainTitle;

    /// <summary>
    /// Summary: sub title from Step 4.
    /// </summary>
    public string SubTitle         => _wizard.MetadataForm.SubTitle;

    /// <summary>
    /// Summary: description from Step 4.
    /// </summary>
    public string Description      => _wizard.MetadataForm.Description;

    /// <summary>
    /// Summary: original tape ID from Step 4.
    /// </summary>
    public string OriginalTapeId   => _wizard.MetadataForm.OriginalTapeId;

    /// <summary>
    /// Initiates video processing.
    /// Full implementation added in M5 — shows a placeholder message in M3.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void ProcessVideo()
    {
        MessageBox.Show(
            "Processing will be implemented in M5.",
            "HVS Capture One",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
