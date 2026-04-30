using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HVSCaptureOne.App.ViewModels;

namespace HVSCaptureOne.App.ViewModels.Wizard;

/// <summary>
/// Manages the multi-step New Project wizard.
/// Owns all step ViewModels and controls forward/back navigation between them.
/// </summary>
public partial class ProjectWizardViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly List<ObservableObject> _steps;

    /// <summary>
    /// The ViewModel for the currently visible wizard step.
    /// Bound to a ContentControl in ProjectWizardView.xaml.
    /// </summary>
    [ObservableProperty]
    private ObservableObject _currentStep = null!;

    /// <summary>
    /// Zero-based index of the current step.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepLabel))]
    [NotifyPropertyChangedFor(nameof(IsFirstStep))]
    [NotifyPropertyChangedFor(nameof(IsLastStep))]
    private int _currentStepIndex;

    /// <summary>
    /// Total number of steps in the wizard.
    /// </summary>
    public int StepCount => _steps.Count;

    /// <summary>
    /// Human-readable step indicator (e.g. "Step 2 of 5").
    /// </summary>
    public string StepLabel => $"Step {CurrentStepIndex + 1} of {StepCount}";

    /// <summary>
    /// True when the wizard is on the first step (Back button hidden).
    /// </summary>
    public bool IsFirstStep => CurrentStepIndex == 0;

    /// <summary>
    /// True when the wizard is on the last step (Next button hidden).
    /// </summary>
    public bool IsLastStep => CurrentStepIndex == StepCount - 1;

    /// <summary>
    /// The step ViewModels shared across steps for data access.
    /// </summary>
    public ProjectInfoStepViewModel ProjectInfo { get; }
    public FileImportStepViewModel FileImport { get; }
    public FileNamingStepViewModel FileNaming { get; }
    public MetadataFormStepViewModel MetadataForm { get; }
    public ReviewStepViewModel Review { get; }

    /// <summary>
    /// Initializes the wizard and all step ViewModels.
    /// </summary>
    /// <returns></returns>
    public ProjectWizardViewModel(MainViewModel main)
    {
        _main = main;

        ProjectInfo  = new ProjectInfoStepViewModel(this);
        FileImport   = new FileImportStepViewModel(this);
        FileNaming   = new FileNamingStepViewModel(this);
        MetadataForm = new MetadataFormStepViewModel(this);
        Review       = new ReviewStepViewModel(this, main);

        _steps = new List<ObservableObject>
        {
            ProjectInfo,
            FileImport,
            FileNaming,
            MetadataForm,
            Review
        };

        CurrentStep = _steps[0];
    }

    /// <summary>
    /// Advances to the next wizard step if the current step is valid.
    /// </summary>
    /// <returns></returns>
    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        if (CurrentStepIndex >= StepCount - 1) return;
        CurrentStepIndex++;
        CurrentStep = _steps[CurrentStepIndex];
    }

    /// <summary>
    /// Returns true when the Next command can execute.
    /// </summary>
    /// <returns></returns>
    private bool CanGoNext() => !IsLastStep && CurrentStepIsValid();

    /// <summary>
    /// Returns to the previous wizard step.
    /// </summary>
    /// <returns></returns>
    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back()
    {
        if (CurrentStepIndex <= 0) return;
        CurrentStepIndex--;
        CurrentStep = _steps[CurrentStepIndex];
    }

    /// <summary>
    /// Returns true when the Back command can execute.
    /// </summary>
    /// <returns></returns>
    private bool CanGoBack() => !IsFirstStep;

    /// <summary>
    /// Cancels the wizard and returns to the Dashboard.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void Cancel() => _main.NavigateTo(new DashboardViewModel(_main));

    /// <summary>
    /// Notifies the Next and Back commands to re-evaluate their CanExecute state.
    /// Called by step ViewModels when their validation state changes.
    /// </summary>
    /// <returns></returns>
    public void RefreshNavigation()
    {
        NextCommand.NotifyCanExecuteChanged();
        BackCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Checks whether the current step has valid data to proceed.
    /// </summary>
    /// <returns></returns>
    private bool CurrentStepIsValid()
    {
        return CurrentStep switch
        {
            ProjectInfoStepViewModel vm  => vm.IsValid,
            FileImportStepViewModel  vm  => vm.IsValid,
            FileNamingStepViewModel  vm  => vm.IsValid,
            MetadataFormStepViewModel vm => vm.IsValid,
            _                           => true
        };
    }
}
