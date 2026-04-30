using CommunityToolkit.Mvvm.ComponentModel;

namespace HVSCaptureOne.App.ViewModels.Wizard;

/// <summary>
/// ViewModel for Step 4 of the New Project wizard: metadata entry.
/// Collects all business metadata fields that will be embedded as atoms in the output MP4.
/// </summary>
public partial class MetadataFormStepViewModel : ObservableObject
{
    private readonly ProjectWizardViewModel _wizard;

    /// <summary>
    /// Initializes Step 4 with a reference to the parent wizard.
    /// </summary>
    /// <returns></returns>
    public MetadataFormStepViewModel(ProjectWizardViewModel wizard)
    {
        _wizard = wizard;
    }

    /// <summary>
    /// Main display title — embedded as both ttl1 and dvat atoms.
    /// Required field.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _mainTitle = string.Empty;

    /// <summary>
    /// Sub display title — embedded as ttls atom.
    /// </summary>
    [ObservableProperty] private string _subTitle = string.Empty;

    /// <summary>
    /// Video description — embedded as ttld atom.
    /// </summary>
    [ObservableProperty] private string _description = string.Empty;

    /// <summary>
    /// Original tape identifier entered by the studio (e.g. "Tape 12, Box A-3").
    /// Stored in the project model only — not embedded as an atom.
    /// </summary>
    [ObservableProperty] private string _originalTapeId = string.Empty;

    /// <summary>
    /// Internal studio notes — never embedded in the MP4 output.
    /// </summary>
    [ObservableProperty] private string _notes = string.Empty;

    /// <summary>
    /// True when all required metadata fields are filled and the step can proceed.
    /// </summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(MainTitle);

    partial void OnMainTitleChanged(string value) => _wizard.RefreshNavigation();
}
