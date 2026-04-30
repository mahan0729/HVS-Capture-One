namespace HVSCaptureOne.Core.Models;

public class UserProfile
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Fixed HVS franchise/location constant — maps to atom cpnm.
    // Not user-editable in the UI.
    public string HVSLocationNumber { get; set; } = "55";
}
