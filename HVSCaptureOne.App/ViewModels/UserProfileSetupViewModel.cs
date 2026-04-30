using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HVSCaptureOne.Core.Models;
using HVSCaptureOne.Core.Services;

namespace HVSCaptureOne.App.ViewModels;

/// <summary>
/// ViewModel for the first-launch user profile setup screen.
/// Shown once when no profile exists in AppData.
/// </summary>
public partial class UserProfileSetupViewModel : ObservableObject
{
    private readonly UserProfileService _profileService;
    private readonly MainViewModel _main;

    /// <summary>
    /// Initializes the setup ViewModel with required services.
    /// </summary>
    /// <returns></returns>
    public UserProfileSetupViewModel(MainViewModel main, UserProfileService profileService)
    {
        _main = main;
        _profileService = profileService;
    }

    /// <summary>Gets or sets the user's first name.</summary>
    [ObservableProperty] private string _firstName = string.Empty;

    /// <summary>Gets or sets the user's last name.</summary>
    [ObservableProperty] private string _lastName = string.Empty;

    /// <summary>Gets or sets the studio company name.</summary>
    [ObservableProperty] private string _companyName = string.Empty;

    /// <summary>Gets or sets the studio address.</summary>
    [ObservableProperty] private string _address = string.Empty;

    /// <summary>Gets or sets the studio phone number.</summary>
    [ObservableProperty] private string _phone = string.Empty;

    /// <summary>Gets or sets the studio email address.</summary>
    [ObservableProperty] private string _email = string.Empty;

    /// <summary>Gets or sets a validation message shown when required fields are missing.</summary>
    [ObservableProperty] private string _validationMessage = string.Empty;

    /// <summary>
    /// Saves the profile and navigates to the Dashboard.
    /// Validates that required fields are filled before saving.
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private void SaveAndContinue()
    {
        if (string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName))
        {
            ValidationMessage = "First name and last name are required.";
            return;
        }

        var profile = new UserProfile
        {
            FirstName = FirstName.Trim(),
            LastName = LastName.Trim(),
            CompanyName = CompanyName.Trim(),
            Address = Address.Trim(),
            Phone = Phone.Trim(),
            Email = Email.Trim(),
            HVSLocationNumber = "55"
        };

        _profileService.Save(profile);
        _main.NavigateTo(new DashboardViewModel(_main));
    }
}
