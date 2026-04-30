using System.Text.Json;
using HVSCaptureOne.Core.Models;

namespace HVSCaptureOne.Core.Services;

public class ProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Gets the full path to the folder where project JSON files are stored.
    /// </summary>
    /// <returns></returns>
    public string ProjectsFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HVSCaptureOne",
        "projects");

    /// <summary>
    /// Returns the full file path for a given project ID.
    /// </summary>
    /// <returns></returns>
    private string ProjectFilePath(string projectId) =>
        Path.Combine(ProjectsFolder, $"{projectId}.json");

    /// <summary>
    /// Saves a project to disk as JSON.
    /// Creates the projects folder if it does not yet exist.
    /// </summary>
    /// <returns></returns>
    public void Save(Project project)
    {
        Directory.CreateDirectory(ProjectsFolder);
        string json = JsonSerializer.Serialize(project, JsonOptions);
        File.WriteAllText(ProjectFilePath(project.ProjectId), json);
    }

    /// <summary>
    /// Loads a project by its project ID.
    /// Returns null if no matching file is found.
    /// </summary>
    /// <returns></returns>
    public Project? Load(string projectId)
    {
        string path = ProjectFilePath(projectId);
        if (!File.Exists(path))
            return null;

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Project>(json, JsonOptions);
    }

    /// <summary>
    /// Loads all saved projects from the projects folder.
    /// Returns an empty list if the folder does not exist.
    /// </summary>
    /// <returns></returns>
    public List<Project> LoadAll()
    {
        if (!Directory.Exists(ProjectsFolder))
            return new List<Project>();

        var projects = new List<Project>();
        foreach (string file in Directory.GetFiles(ProjectsFolder, "*.json"))
        {
            string json = File.ReadAllText(file);
            var project = JsonSerializer.Deserialize<Project>(json, JsonOptions);
            if (project is not null)
                projects.Add(project);
        }
        return projects;
    }

    /// <summary>
    /// Deletes the JSON file for the given project ID.
    /// Does nothing if the file does not exist.
    /// </summary>
    /// <returns></returns>
    public void Delete(string projectId)
    {
        string path = ProjectFilePath(projectId);
        if (File.Exists(path))
            File.Delete(path);
    }
}
