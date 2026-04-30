using CommunityToolkit.Mvvm.ComponentModel;

namespace HVSCaptureOne.App.ViewModels.Wizard;

/// <summary>
/// ViewModel for Step 3 of the New Project wizard: file naming.
/// Presents the auto-generated filename and allows the user to override it.
/// </summary>
public partial class FileNamingStepViewModel : ObservableObject
{
    private readonly ProjectWizardViewModel _wizard;

    /// <summary>
    /// Initializes Step 3 with a reference to the parent wizard.
    /// </summary>
    /// <returns></returns>
    public FileNamingStepViewModel(ProjectWizardViewModel wizard)
    {
        _wizard = wizard;
    }

    /// <summary>Gets or sets whether the user keeps the suggested filename.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private bool _useSuggestedName = true;

    /// <summary>Gets or sets the custom filename entered by the user when overriding.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _customFileName = string.Empty;

    /// <summary>
    /// The auto-generated filename, derived from the client name entered in Step 1.
    /// Format: LastName_FirstName_01
    /// </summary>
    public string SuggestedFileName =>
        !string.IsNullOrEmpty(_wizard.ProjectInfo.ClientLastName)
            ? $"{Capitalize(_wizard.ProjectInfo.ClientLastName)}_{Capitalize(_wizard.ProjectInfo.ClientFirstName)}_01"
            : "Client_Name_01";

    /// <summary>
    /// The filename that will actually be used — either suggested or custom.
    /// </summary>
    public string EffectiveFileName =>
        UseSuggestedName ? SuggestedFileName : CustomFileName.Trim();

    /// <summary>
    /// True when the step has a valid filename to proceed with.
    /// </summary>
    public bool IsValid =>
        UseSuggestedName
            ? !string.IsNullOrEmpty(SuggestedFileName)
            : !string.IsNullOrWhiteSpace(CustomFileName);

    /// <summary>
    /// Capitalizes the first letter of a name segment.
    /// </summary>
    /// <returns></returns>
    private static string Capitalize(string value) =>
        string.IsNullOrEmpty(value) ? value :
        char.ToUpper(value[0]) + value[1..].ToLower();

    partial void OnUseSuggestedNameChanged(bool value) => _wizard.RefreshNavigation();
    partial void OnCustomFileNameChanged(string value)  => _wizard.RefreshNavigation();
}
