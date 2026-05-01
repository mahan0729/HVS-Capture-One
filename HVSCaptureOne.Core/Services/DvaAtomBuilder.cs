using System.Globalization;
using HVSCaptureOne.Core.Atoms;
using HVSCaptureOne.Core.Models;

namespace HVSCaptureOne.Core.Services;

/// <summary>
/// Builds the ordered list of DVA uuid atom payloads for a given project and asset.
/// Atom values are derived from user-entered metadata, the detected video duration,
/// the operator profile, and the processing timestamp.
/// </summary>
public class DvaAtomBuilder
{
    /// <summary>
    /// Produces the complete atom list for the output MP4.
    /// Atoms are ordered to match the sequence observed in known-good DVA files.
    /// </summary>
    /// <returns>Ordered list of atom name/value pairs ready for Mp4BoxWriter.</returns>
    public IReadOnlyList<DvaAtomPayload> Build(
        Project project,
        VideoAsset asset,
        UserProfile operatorProfile,
        DateTime processedAt)
    {
        var meta = asset.Metadata;

        var runningMinutes = (int)Math.Round(meta.DetectedDuration.TotalMinutes);
        var plen = $"Running Time: {runningMinutes} minutes";

        // DVA date format confirmed from forensics: "MM/DD/YYYY HH:MM:SS AM/PM"
        var date = processedAt.ToString("MM/dd/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);

        var atoms = new List<DvaAtomPayload>
        {
            new(AtomCanon.MainTitle,      meta.MainTitle),
            new(AtomCanon.MainTitleDva,   meta.MainTitle),
            new(AtomCanon.SubTitle,       meta.SubTitle),
            new(AtomCanon.Description,    meta.Description),
            new(AtomCanon.ProgramLength,  plen),
            new(AtomCanon.CreationDate,   date),
            new(AtomCanon.ClientName,     project.ClientName),
            new(AtomCanon.ClientEmail,    project.ClientEmail),
            new(AtomCanon.LocationNumber, operatorProfile.HVSLocationNumber),
            new(AtomCanon.ProjectId,      project.ProjectId),
            new(AtomCanon.ChapterCount,   meta.Chapters.Count.ToString()),
        };

        // ch01..chNN — populated in v1.5 when chapters are supported
        foreach (var (chapter, index) in meta.Chapters.Select((c, i) => (c, i + 1)))
            atoms.Add(new(AtomCanon.GetChapterAtomName(index), chapter));

        return atoms.AsReadOnly();
    }
}
