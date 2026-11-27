using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace TheLastGate.Notes;

// "TLGA", then little-endian u32 format, payload size and compressed size, then the zlib-compressed payload: a
// show-on-minimap byte and the category tree. Formats 1 and 2, read but never written, hold a varint category count
// and for each a visible byte, a show-labels byte (format 2 only) and the category body without note colours.
public static class NoteStore
{
    private const uint CurrentFormat = 3;
    private const uint FirstFormat = 1;
    private const uint FirstTreeFormat = 3;
    private const uint FirstLabelsFormat = 2;
    private const int HeaderSize = 16;
    private const int LargestPayload = 16 * 1024 * 1024;
    private const string TemporarySuffix = ".tmp";
    private const string SetAsideSuffix = ".bad";
    private const string FirstCategoryName = "My notes";
    private const int FirstCategoryColour = 1;
    private const byte MinimapFlag = 1;
    private const byte WorldFlag = 2;

    private static ReadOnlySpan<byte> Magic => "TLGA"u8;

    public static byte[] Save(NoteSet set)
    {
        var payload = new List<byte>
        {
            (byte)((set.ShowOnMinimap ? MinimapFlag : 0) | (set.ShowInWorld ? WorldFlag : 0)),
        };
        NoteCodec.EncodeTree(set, 0, payload);

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, true))
        {
            zlib.Write(payload.ToArray());
        }

        var file = new byte[HeaderSize + compressed.Length];
        Magic.CopyTo(file);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), CurrentFormat);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(8), (uint)payload.Count);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(12), (uint)compressed.Length);
        compressed.GetBuffer().AsSpan(0, (int)compressed.Length).CopyTo(file.AsSpan(HeaderSize));
        return file;
    }

    public static NoteSet? Load(ReadOnlySpan<byte> file)
    {
        if (file.Length < HeaderSize || !file[..Magic.Length].SequenceEqual(Magic))
        {
            return null;
        }

        var format = BinaryPrimitives.ReadUInt32LittleEndian(file[4..]);
        var payloadSize = BinaryPrimitives.ReadUInt32LittleEndian(file[8..]);
        var compressedSize = BinaryPrimitives.ReadUInt32LittleEndian(file[12..]);
        if (format is < FirstFormat or > CurrentFormat
            || payloadSize is 0 or > LargestPayload
            || compressedSize == 0
            || compressedSize > CompressBound(payloadSize)
            || compressedSize > file.Length - HeaderSize)
        {
            return null;
        }

        if (Inflate(file.Slice(HeaderSize, (int)compressedSize), (int)payloadSize) is not { } payload)
        {
            return null;
        }

        var set = format >= FirstTreeFormat ? ParseTree(payload) : ParseLegacy(payload, format);
        if (set != null)
        {
            set.Changed = false;
        }

        return set;
    }

    // Written beside the old file and swapped in, so a crash mid-save never leaves half a file.
    public static bool SaveTo(string path, NoteSet set)
    {
        var temporaryPath = path + TemporarySuffix;
        try
        {
            File.WriteAllBytes(temporaryPath, Save(set));
            File.Move(temporaryPath, path, true);
            set.Changed = false;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            File.Delete(temporaryPath);
            return false;
        }
    }

    // The current client's file is read once, to start from, and never moved; only this client's own damaged file is
    // set aside as .bad. An empty set starts with the current client's first category, not counted as a change.
    public static (NoteSet Set, bool SetAside) Open(string ownPath, string sdlClientPath)
    {
        var setAside = false;
        NoteSet? set = null;
        if (File.Exists(ownPath))
        {
            if (TryReadFile(ownPath) is { } ownFile)
            {
                set = Load(ownFile);
                setAside = set == null && TrySetAside(ownPath);
            }
        }
        else if (File.Exists(sdlClientPath) && TryReadFile(sdlClientPath) is { } sdlClientFile)
        {
            set = Load(sdlClientFile);
        }

        set ??= new NoteSet();
        if (set.Categories.Count == 0)
        {
            set.AddCategory(FirstCategoryName, FirstCategoryColour);
            set.Changed = false;
        }

        return (set, setAside);
    }

    // zlib's own bound on how big compressing a payload can make it.
    private static long CompressBound(uint size) => size + (size >> 12) + (size >> 14) + (size >> 25) + 13;

    private static byte[]? Inflate(ReadOnlySpan<byte> compressed, int expectedSize)
    {
        try
        {
            using var zlib = new ZLibStream(new MemoryStream(compressed.ToArray()), CompressionMode.Decompress);
            var payload = new byte[expectedSize];
            zlib.ReadExactly(payload);
            return zlib.ReadByte() == -1 ? payload : null;
        }
        catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException)
        {
            return null;
        }
    }

    private static NoteSet? ParseTree(byte[] payload)
    {
        var offset = 1;
        if (NoteCodec.DecodeTree(payload, ref offset, false, out var tree) != NoteDecodeResult.Decoded
            || offset != payload.Length)
        {
            return null;
        }

        tree!.ShowOnMinimap = (payload[0] & MinimapFlag) != 0;
        tree.ShowInWorld = (payload[0] & WorldFlag) != 0;
        return tree;
    }

    private static NoteSet? ParseLegacy(byte[] payload, uint format)
    {
        var set = new NoteSet { ShowOnMinimap = payload[0] != 0 };
        var offset = 1;
        if (!NoteCodec.TryReadVarint(payload, ref offset, out var categoryCount)
            || categoryCount > NoteLimits.Categories)
        {
            return null;
        }

        for (var index = 0; index < categoryCount; index++)
        {
            var flagsSize = format >= FirstLabelsFormat ? 2 : 1;
            if (payload.Length - offset < flagsSize)
            {
                return null;
            }

            var visible = payload[offset] != 0;
            var showLabels = format < FirstLabelsFormat || payload[offset + 1] != 0;
            offset += flagsSize;
            if (NoteCodec.DecodeCategory(payload, ref offset, out var category) != NoteDecodeResult.Decoded)
            {
                return null;
            }

            category!.Visible = visible;
            category.ShowLabels = showLabels;
            if (set.Attach(category, 0) == 0)
            {
                return null;
            }
        }

        return offset == payload.Length ? set : null;
    }

    private static byte[]? TryReadFile(string path)
    {
        try
        {
            return File.ReadAllBytes(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool TrySetAside(string path)
    {
        try
        {
            File.Move(path, path + SetAsideSuffix, true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
