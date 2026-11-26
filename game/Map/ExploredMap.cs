using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace TheLastGate.Map;

// Every tile the player has seen, by its average colour. Tiles are stored a column at a time: tile (x, y) is at
// x * height + y.
public sealed class ExploredMap
{
    public const int Size = 2048;
    public const int BlockSize = 256;

    // "TLGM", then little-endian u32 format, width, height and compressed size, then the zlib-compressed tiles.
    // Format 1 tiles are u16 RGB555, format 2 tiles are u32 0x00RRGGBB.
    private const int HeaderSize = 20;
    private const uint Rgb555Format = 1;
    private const uint FullColourFormat = 2;
    private const string TemporarySuffix = ".tmp";

    private readonly int[] blockChanges;
    private readonly int blocksDown;
    private readonly uint[] tiles;
    private int loads;

    public ExploredMap(int width = Size, int height = Size)
    {
        Width = width;
        Height = height;
        tiles = new uint[width * height];
        blocksDown = (height + BlockSize - 1) / BlockSize;
        blockChanges = new int[(width + BlockSize - 1) / BlockSize * blocksDown];
    }

    public int Width { get; }

    public int Height { get; }

    public bool Changed { get; private set; }

    public int Revision { get; private set; }

    private static ReadOnlySpan<byte> Magic => "TLGM"u8;

    // A blank map is written rather than the file deleted, so the current client's map isn't imported over it again.
    public static bool ClearSaved(string path) => new ExploredMap().SaveTo(path);

    public int BlockRevision(int blockX, int blockY) => loads + blockChanges[blockX * blocksDown + blockY];

    public uint ColourAt(int tileX, int tileY) => IsOnMap(tileX, tileY) ? tiles[tileX * Height + tileY] : 0;

    public void SetTile(int tileX, int tileY, uint colour)
    {
        if (!IsOnMap(tileX, tileY) || tiles[tileX * Height + tileY] == colour)
        {
            return;
        }

        tiles[tileX * Height + tileY] = colour;
        blockChanges[tileX / BlockSize * blocksDown + tileY / BlockSize]++;
        Changed = true;
        Revision++;
    }

    public void ClearBox(int left, int top, int boxWidth, int boxHeight)
    {
        var endX = (int)Math.Min((long)left + boxWidth, Width);
        var endY = (int)Math.Min((long)top + boxHeight, Height);
        for (var tileX = Math.Max(left, 0); tileX < endX; tileX++)
        {
            for (var tileY = Math.Max(top, 0); tileY < endY; tileY++)
            {
                SetTile(tileX, tileY, 0);
            }
        }
    }

    public void MarkSaved() => Changed = false;

    // Anything that can't be read leaves a blank map, as the current client does.
    public bool Load(ReadOnlySpan<byte> file)
    {
        var loaded = TryRead(file);
        if (!loaded)
        {
            Array.Clear(tiles);
        }

        Changed = false;
        Revision++;
        loads++;
        return loaded;
    }

    public byte[] Save()
    {
        var tileBytes = new byte[tiles.Length * sizeof(uint)];
        for (var index = 0; index < tiles.Length; index++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(tileBytes.AsSpan(index * sizeof(uint)), tiles[index]);
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, true))
        {
            zlib.Write(tileBytes);
        }

        var file = new byte[HeaderSize + compressed.Length];
        Magic.CopyTo(file);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), FullColourFormat);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(8), (uint)Width);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(12), (uint)Height);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(16), (uint)compressed.Length);
        compressed.GetBuffer().AsSpan(0, (int)compressed.Length).CopyTo(file.AsSpan(HeaderSize));
        return file;
    }

    public bool LoadFrom(string path)
    {
        try
        {
            return Load(File.Exists(path) ? File.ReadAllBytes(path) : []);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return Load([]);
        }
    }

    // Written beside the old file and swapped in, so a crash mid-save never leaves half a map.
    public bool SaveTo(string path)
    {
        var temporaryPath = path + TemporarySuffix;
        try
        {
            File.WriteAllBytes(temporaryPath, Save());
            File.Move(temporaryPath, path, true);
            MarkSaved();
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            File.Delete(temporaryPath);
            return false;
        }
    }

    private bool IsOnMap(int tileX, int tileY) => tileX >= 0 && tileY >= 0 && tileX < Width && tileY < Height;

    private bool TryRead(ReadOnlySpan<byte> file)
    {
        if (file.Length < HeaderSize || !file[..Magic.Length].SequenceEqual(Magic))
        {
            return false;
        }

        var format = BinaryPrimitives.ReadUInt32LittleEndian(file[4..]);
        var tileSize = format switch
        {
            Rgb555Format => sizeof(ushort),
            FullColourFormat => sizeof(uint),
            _ => 0,
        };
        var compressedSize = BinaryPrimitives.ReadUInt32LittleEndian(file[16..]);
        if (tileSize == 0
            || BinaryPrimitives.ReadUInt32LittleEndian(file[8..]) != Width
            || BinaryPrimitives.ReadUInt32LittleEndian(file[12..]) != Height
            || compressedSize == 0
            || compressedSize > file.Length - HeaderSize)
        {
            return false;
        }

        if (Inflate(file.Slice(HeaderSize, (int)compressedSize), tiles.Length * tileSize) is not { } tileBytes)
        {
            return false;
        }

        for (var index = 0; index < tiles.Length; index++)
        {
            tiles[index] = tileSize == sizeof(ushort)
                ? MinimapColours.FromRgb555(BinaryPrimitives.ReadUInt16LittleEndian(tileBytes.AsSpan(index * tileSize)))
                : BinaryPrimitives.ReadUInt32LittleEndian(tileBytes.AsSpan(index * tileSize));
        }

        return true;
    }

    // The tiles must fill exactly the expected size; a stream that ends early or runs on is damaged.
    private static byte[]? Inflate(ReadOnlySpan<byte> compressed, int expectedSize)
    {
        try
        {
            using var zlib = new ZLibStream(new MemoryStream(compressed.ToArray()), CompressionMode.Decompress);
            var tileBytes = new byte[expectedSize];
            zlib.ReadExactly(tileBytes);
            return zlib.ReadByte() == -1 ? tileBytes : null;
        }
        catch (Exception exception) when (exception is EndOfStreamException or InvalidDataException)
        {
            return null;
        }
    }
}
