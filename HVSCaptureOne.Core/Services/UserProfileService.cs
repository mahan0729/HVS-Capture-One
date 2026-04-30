using System.Text.Json;
using HVSCaptureOne.Core.Models;

namespace HVSCaptureOne.Core.Services;

public class UserProfileService
{
    private static readonly string AppDataRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HVSCaptureOne");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Gets the full path to the user profile JSON file.
    /// </summary>
    /// <returns></returns>
    public string ProfilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HVSCaptureOne",
        "user_profile.json");

    /// <summary>
    /// Returns true if a saved user profile exists on disk.
    /// Used at startup to detect first launch.
    /// </summary>
    /// <returns></returns>
    public bool Exists() => File.Exists(ProfilePath);

    /// <summary>
    /// Loads the user profile from disk.
    /// Returns null if no profile file exists (first launch).
    /// </summary>
    /// <returns></returns>
    public UserProfile? Load()
    {
        if (!File.Exists(ProfilePath))
            return null;

        string json = File.ReadAllText(ProfilePath);
        return JsonSerializer.Deserialize<UserProfile>(json, JsonOptions);
    }

    /// <summary>
    /// Saves the user profile to disk as JSON.
    /// Creates the application data directory if it does not yet exist.
    /// </summary>
    /// <returns></returns>
    public void Save(UserProfile profile)
    {
        Directory.CreateDirectory(AppDataRoot);
        string json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(ProfilePath, json);
    }
}
