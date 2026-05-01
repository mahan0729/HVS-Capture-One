using System.Buffers.Binary;
using System.Text;
using HVSCaptureOne.Core.Models;

namespace HVSCaptureOne.Core.Services;

/// <summary>
/// Writes a new MP4 file with DVA uuid atoms injected into moov/udta/meta.
///
/// Output layout is always: ftyp → moov (with udta/meta/uuid atoms) → mdat.
/// Chunk offsets in stco and co64 boxes are adjusted to account for the size
/// change in moov, keeping the output file fully playable.
///
/// The uuid atom binary format (confirmed from DVA file forensics):
///   [4B size big-endian] [4B 'uuid'] [16B random UUID] [4B atom name] [N bytes UTF-8 value]
/// </summary>
public class Mp4BoxWriter
{
    private const int StreamBuffer = 81920; // 80 KB streaming buffer for mdat copy

    // Describes a parsed box header within a file or byte buffer.
    private readonly record struct BoxHeader(
        string Type,
        long   Offset,     // absolute byte offset of the size field
        long   DataOffset, // absolute byte offset of the box content (after size+type header)
        long   TotalSize); // total bytes including header

    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// Reads sourcePath, injects the given DVA atoms, and writes the result to outputPath.
    /// The source file is never modified.
    /// </summary>
    /// <returns></returns>
    public void Write(
        string sourcePath,
        string outputPath,
        IReadOnlyList<DvaAtomPayload> atoms)
    {
        using var source = File.OpenRead(sourcePath);

        // 1. Locate the top-level boxes (ftyp, moov, mdat)
        var topLevel = ReadBoxHeaders(source, 0, source.Length);

        var ftyp = topLevel.First(b => b.Type == "ftyp");
        var moov = topLevel.First(b => b.Type == "moov");
        var mdat = topLevel.First(b => b.Type == "mdat");

        // 2. Read entire moov box into memory (typically 4–10 MB)
        source.Seek(moov.Offset, SeekOrigin.Begin);
        var moovBytes = new byte[moov.TotalSize];
        source.ReadExactly(moovBytes);

        // 3. Build the new udta block (udta → meta fullbox → uuid atoms)
        byte[] udtaBytes = BuildUdta(atoms);

        // 4. Splice new udta into moov, removing any pre-existing udta
        byte[] newMoovBytes = InjectUdta(moovBytes, udtaBytes);

        // 5. Calculate the offset delta
        //    Output layout: [ftyp][newMoov][mdat]
        //    The mdat will move from its old absolute position to ftypSize + newMoovSize.
        long newMdatOffset = ftyp.TotalSize + newMoovBytes.LongLength;
        long delta = newMdatOffset - mdat.Offset;

        // 6. Patch stco / co64 entries in newMoovBytes so players can find the samples
        PatchChunkOffsets(newMoovBytes, delta);

        // 7. Stream the output file
        using var output = File.Create(outputPath);

        source.Seek(ftyp.Offset, SeekOrigin.Begin);
        CopyExact(source, output, ftyp.TotalSize);   // ftyp (from source)

        output.Write(newMoovBytes);                  // new moov

        source.Seek(mdat.Offset, SeekOrigin.Begin);
        CopyExact(source, output, mdat.TotalSize);   // mdat (streamed, not buffered in RAM)
    }

    // ── Box header parsing ────────────────────────────────────────────────────

    /// <summary>
    /// Reads sequential box headers from a stream between [start, end).
    /// Handles normal (32-bit), extended (64-bit), and to-EOF (size==0) boxes.
    /// </summary>
    /// <returns>Ordered list of BoxHeader structs.</returns>
    private static IReadOnlyList<BoxHeader> ReadBoxHeaders(Stream stream, long start, long end)
    {
        var boxes = new List<BoxHeader>();
        stream.Seek(start, SeekOrigin.Begin);

        Span<byte> hdr = stackalloc byte[8];
        Span<byte> ext = stackalloc byte[8];

        while (stream.Position + 8 <= end)
        {
            long boxStart = stream.Position;

            stream.ReadExactly(hdr);

            uint   size32 = BinaryPrimitives.ReadUInt32BigEndian(hdr[..4]);
            string type   = Encoding.ASCII.GetString(hdr[4..]);

            long totalSize;
            long dataOffset;

            if (size32 == 1)
            {
                // Extended 64-bit size field immediately follows the 8-byte header
                stream.ReadExactly(ext);
                totalSize  = (long)BinaryPrimitives.ReadUInt64BigEndian(ext);
                dataOffset = boxStart + 16;
            }
            else if (size32 == 0)
            {
                // Box runs to end of the containing scope
                totalSize  = end - boxStart;
                dataOffset = boxStart + 8;
            }
            else
            {
                totalSize  = size32;
                dataOffset = boxStart + 8;
            }

            boxes.Add(new BoxHeader(type, boxStart, dataOffset, totalSize));
            stream.Seek(boxStart + totalSize, SeekOrigin.Begin);
        }

        return boxes;
    }

    // ── uuid atom / udta builder ──────────────────────────────────────────────

    /// <summary>
    /// Builds the raw bytes for a single DVA uuid atom.
    /// Format: [4B size][4B 'uuid'][16B random UUID][4B name][N bytes UTF-8 value]
    /// </summary>
    /// <returns>Complete uuid box as a byte array.</returns>
    private static byte[] BuildUuidAtom(string name, string value)
    {
        byte[] nameBytes  = Encoding.ASCII.GetBytes(name);
        byte[] valueBytes = Encoding.UTF8.GetBytes(value);
        byte[] uuidBytes  = Guid.NewGuid().ToByteArray(); // 16 random bytes

        int totalSize = 4 + 4 + 16 + nameBytes.Length + valueBytes.Length;
        var box = new byte[totalSize];

        BinaryPrimitives.WriteUInt32BigEndian(box.AsSpan(0, 4), (uint)totalSize);
        Encoding.ASCII.GetBytes("uuid").CopyTo(box, 4);
        uuidBytes.CopyTo(box, 8);
        nameBytes.CopyTo(box, 24);
        valueBytes.CopyTo(box, 28);

        return box;
    }

    /// <summary>
    /// Builds the complete udta block containing a meta fullbox with all uuid atoms.
    /// Structure: udta → meta(version=0,flags=0) → [uuid atoms...]
    /// </summary>
    /// <returns>udta box as a byte array.</returns>
    private static byte[] BuildUdta(IReadOnlyList<DvaAtomPayload> atoms)
    {
        byte[][] uuidAtoms = atoms
            .Select(a => BuildUuidAtom(a.Name, a.Value))
            .ToArray();

        int uuidTotal = uuidAtoms.Sum(a => a.Length);

        // meta fullbox header: 4B size + 4B 'meta' + 4B version+flags = 12 bytes
        int metaSize = 12 + uuidTotal;
        // udta box header: 4B size + 4B 'udta' = 8 bytes
        int udtaSize = 8 + metaSize;

        var udta = new byte[udtaSize];
        int pos = 0;

        // udta header
        BinaryPrimitives.WriteUInt32BigEndian(udta.AsSpan(pos, 4), (uint)udtaSize);
        Encoding.ASCII.GetBytes("udta").CopyTo(udta, pos + 4);
        pos += 8;

        // meta fullbox header (version=0, flags=0,0,0)
        BinaryPrimitives.WriteUInt32BigEndian(udta.AsSpan(pos, 4), (uint)metaSize);
        Encoding.ASCII.GetBytes("meta").CopyTo(udta, pos + 4);
        pos += 12; // 8-byte box header + 4-byte version+flags (all zeros)

        // uuid atoms
        foreach (var atom in uuidAtoms)
        {
            atom.CopyTo(udta, pos);
            pos += atom.Length;
        }

        return udta;
    }

    // ── moov splice ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a new moov byte array with any existing udta removed and the
    /// supplied udta bytes appended as the last child of moov.
    /// The moov size field is updated to reflect the new total length.
    /// </summary>
    /// <returns>New moov box bytes.</returns>
    private static byte[] InjectUdta(byte[] moovBytes, byte[] udtaBytes)
    {
        // Scan moov children (moov header is 8 bytes) to find existing udta
        int removeStart = -1;
        int removeEnd   = -1;

        int pos = 8;
        while (pos + 8 <= moovBytes.Length)
        {
            int childSize = (int)BinaryPrimitives.ReadUInt32BigEndian(moovBytes.AsSpan(pos, 4));
            if (childSize < 8) break;

            string childType = Encoding.ASCII.GetString(moovBytes, pos + 4, 4);

            if (childType == "udta")
            {
                removeStart = pos;
                removeEnd   = pos + childSize;
                break;
            }

            pos += childSize;
        }

        // Calculate new moov size
        int removedBytes = removeStart >= 0 ? (removeEnd - removeStart) : 0;
        int newMoovSize  = moovBytes.Length - removedBytes + udtaBytes.Length;

        var newMoov = new byte[newMoovSize];

        // Write updated moov header (size only; 'moov' type stays the same)
        BinaryPrimitives.WriteUInt32BigEndian(newMoov.AsSpan(0, 4), (uint)newMoovSize);
        Encoding.ASCII.GetBytes("moov").CopyTo(newMoov, 4);

        if (removeStart >= 0)
        {
            // Copy children before udta
            int beforeLen = removeStart - 8;
            Buffer.BlockCopy(moovBytes, 8, newMoov, 8, beforeLen);

            // Copy children after udta
            int afterLen = moovBytes.Length - removeEnd;
            Buffer.BlockCopy(moovBytes, removeEnd, newMoov, 8 + beforeLen, afterLen);

            // Append new udta
            udtaBytes.CopyTo(newMoov, 8 + beforeLen + afterLen);
        }
        else
        {
            // No existing udta — copy all existing children, then append
            Buffer.BlockCopy(moovBytes, 8, newMoov, 8, moovBytes.Length - 8);
            udtaBytes.CopyTo(newMoov, moovBytes.Length);
        }

        return newMoov;
    }

    // ── stco / co64 patching ──────────────────────────────────────────────────

    /// <summary>
    /// Adjusts all stco (32-bit) and co64 (64-bit) chunk offset entries in the
    /// moov byte array by adding delta to each entry.
    /// Recurses into container boxes (trak, mdia, minf, stbl).
    /// </summary>
    /// <returns></returns>
    private static void PatchChunkOffsets(byte[] moovBytes, long delta)
    {
        PatchOffsets(moovBytes, 8, moovBytes.Length, delta);
    }

    /// <summary>
    /// Recursively scans a region of a byte array for stco / co64 boxes
    /// and adjusts their offset tables by delta.
    /// </summary>
    /// <returns></returns>
    private static void PatchOffsets(byte[] data, int start, int end, long delta)
    {
        int pos = start;
        while (pos + 8 <= end)
        {
            int size = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos, 4));
            if (size < 8) break;

            string type = Encoding.ASCII.GetString(data, pos + 4, 4);

            if (type == "stco")
            {
                // stco: [4 size][4 'stco'][4 version+flags][4 count][4*n offsets]
                int count = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos + 12, 4));
                for (int i = 0; i < count; i++)
                {
                    int at  = pos + 16 + i * 4;
                    uint old = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(at, 4));
                    BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(at, 4), (uint)(old + delta));
                }
            }
            else if (type == "co64")
            {
                // co64: [4 size][4 'co64'][4 version+flags][4 count][8*n offsets]
                int count = (int)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos + 12, 4));
                for (int i = 0; i < count; i++)
                {
                    int at    = pos + 16 + i * 8;
                    ulong old = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(at, 8));
                    BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(at, 8), old + (ulong)delta);
                }
            }
            else if (type is "trak" or "mdia" or "minf" or "stbl" or "udta" or "meta")
            {
                // Recurse into known container boxes
                int dataStart = type == "meta" ? pos + 12 : pos + 8; // meta is a fullbox (+4 for version+flags)
                PatchOffsets(data, dataStart, pos + size, delta);
            }

            pos += size;
        }
    }

    // ── Streaming copy ────────────────────────────────────────────────────────

    /// <summary>
    /// Copies exactly 'bytes' bytes from source to dest using a fixed-size buffer.
    /// Used to stream mdat without loading it into RAM.
    /// </summary>
    /// <returns></returns>
    private static void CopyExact(Stream source, Stream dest, long bytes)
    {
        var    buffer    = new byte[StreamBuffer];
        long   remaining = bytes;

        while (remaining > 0)
        {
            int toRead = (int)Math.Min(buffer.Length, remaining);
            source.ReadExactly(buffer, 0, toRead);
            dest.Write(buffer, 0, toRead);
            remaining -= toRead;
        }
    }
}
