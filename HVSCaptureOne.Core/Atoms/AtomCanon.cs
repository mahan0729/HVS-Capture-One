namespace HVSCaptureOne.Core.Atoms;

/// <summary>
/// Atom Canon v1 — confirmed 2026-04-30, approved by Robert Hanley.
/// Defines the exact 4-character atom names used in DVA output MP4 files.
/// All custom atoms are stored as uuid boxes inside moov/udta/meta.
/// </summary>
public static class AtomCanon
{
    // Title — both atoms always receive the same value
    public const string MainTitle    = "ttl1";
    public const string MainTitleDva = "dvat";  // DVA internal copy of ttl1

    public const string SubTitle     = "ttls";
    public const string Description  = "ttld";

    // Program length — format: "Running Time: N minutes"
    public const string ProgramLength = "plen";

    // DVA creation date — format: "MM/DD/YYYY HH:MM:SS AM/PM"
    public const string CreationDate  = "date";

    public const string ClientName    = "unam";
    public const string ClientEmail   = "emal";

    // HVS franchise/location number — fixed constant ("55"), not user-entered
    public const string LocationNumber = "cpnm";

    // Project ID — user-assigned (e.g. "pasotti_sandy01") maps to tpnm atom
    public const string ProjectId     = "tpnm";

    // Chapters (v1.5+)
    public const string ChapterCount  = "numc";

    // Chapter atom names are ch01, ch02... ch26 etc.
    // Use GetChapterAtomName(int n) to generate the correct name.
    // Timestamp format: HH:MM:SS:FF (e.g. "00:04:38:09")
    public const string ChapterPrefix = "ch";

    /// <summary>
    /// Returns the atom name for a given chapter number.
    /// For example, chapter 3 returns "ch03".
    /// </summary>
    /// <returns></returns>
    public static string GetChapterAtomName(int chapterNumber)
        => $"ch{chapterNumber:D2}";

    // NOTE: wcnt and pcnt are sequential counters assigned by the DVA Cloud
    // on upload. HVS Capture One does NOT write these atoms.
}
