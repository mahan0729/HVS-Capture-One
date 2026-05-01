namespace HVSCaptureOne.Core.Models;

/// <summary>
/// Represents a single DVA uuid atom to be written into the output MP4.
/// The atom is stored as a uuid box in moov/udta/meta with the layout:
/// [4B size][4B 'uuid'][16B random UUID][4B name][N bytes UTF-8 value].
/// </summary>
/// <param name="Name">The 4-character atom name (e.g. "ttl1", "dvat").</param>
/// <param name="Value">The UTF-8 string value to store in the atom.</param>
public record DvaAtomPayload(string Name, string Value);
