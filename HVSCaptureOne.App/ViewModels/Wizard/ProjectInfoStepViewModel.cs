using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace HVSCaptureOne.App.ViewModels.Wizard;

/// <summary>
/// ViewModel for Step 1 of the New Project wizard: project and client information.
/// </summary>
public partial class ProjectInfoStepViewModel : ObservableObject
{
    private readonly ProjectWizardViewModel _wizard;

    /// <summary>
    /// Initializes Step 1 with a reference to the parent wizard.
    /// </summary>
    /// <returns></returns>
    public ProjectInfoStepViewModel(ProjectWizardViewModel wizard)
    {
        _wizard = wizard;
    }

    /// <summary>Gets or sets the project name entered by the user.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _projectName = string.Empty;

    /// <summary>Gets or sets the client's first name.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(GeneratedProjectId))]
    private string _clientFirstName = string.Empty;

    /// <summary>Gets or sets the client's last name.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(GeneratedProjectId))]
    private string _clientLastName = string.Empty;

    /// <summary>Gets or sets the client's email address.</summary>
    [ObservableProperty] private string _clientEmail = string.Empty;

    /// <summary>Gets or sets the selected output folder path.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _outputFolder = string.Empty;

    /// <summary>
    /// Auto-generated Project ID based on client name (e.g. "hanley_robert_01").
    /// Maps to the tpnm atom.
    /// </summary>
    public string GeneratedProjectId =>
        string.IsNullOrWhiteSpace(ClientLastName) && string.IsNullOrWhiteSpace(ClientFirstName)
            ? string.Empty
            : $"{ClientLastName.Trim().ToLower().Replace(" ", "")}_{ClientFirstName.Trim().ToLower().Replace(" ", "")}_01";

    /// <summary>
    /// True when all required fields are filled and the step can proceed.
    /// </summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(ProjectName) &&
        !string.IsNullOrWhiteSpace(ClientFirstName) &&
        !string.IsNullOrWhiteSpace(ClientLastName) &&
        !string.IsNullOrWhiteSpace(OutputFolder);

    /// <summary>
    /// Opens a folder browser dialog for the user to select an output folder.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void BrowseOutputFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Output Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            OutputFolder = dialog.FolderName;
            _wizard.RefreshNavigation();
        }
    }

    /// <summary>
    /// Called when any observable property changes to refresh wizard navigation state.
    /// </summary>
    /// <returns></returns>
    partial void OnProjectNameChanged(string value)    => _wizard.RefreshNavigation();
    partial void OnClientFirstNameChanged(string value) => _wizard.RefreshNavigation();
    partial void OnClientLastNameChanged(string value)  => _wizard.RefreshNavigation();
    partial void OnOutputFolderChanged(string value)    => _wizard.RefreshNavigation();
}
