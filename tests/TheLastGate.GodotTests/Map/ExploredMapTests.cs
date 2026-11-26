using System.Buffers.Binary;
using System.IO.Compression;
using GdUnit4;
using TheLastGate.Map;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
public sealed class ExploredMapTests
{
    private const int Width = 64;
    private const int Height = 32;
    private const int HeaderSize = 20;

    private string folder = "";

    [BeforeTest]
    public void MakeAFolder() => folder = Directory.CreateTempSubdirectory("explored-map-").FullName;

    [AfterTest]
    public void DeleteTheFolder() => Directory.Delete(folder, true);

    [TestCase]
    public void ANewMap_IsTheCurrentClientsSizeAndUnexplored()
    {
        var map = new ExploredMap();

        AssertThat(map.Width).IsEqual(2048);
        AssertThat(map.Height).IsEqual(2048);
        AssertThat(map.ColourAt(1000, 1000)).IsEqual(0u);
        AssertThat(map.Changed).IsFalse();
    }

    [TestCase]
    public void SetTile_MarksTheMapChangedOnlyWhenTheColourDiffers()
    {
        var map = new ExploredMap(Width, Height);

        map.SetTile(3, 4, 0x1234);
        AssertThat(map.ColourAt(3, 4)).IsEqual(0x1234u);
        AssertThat(map.Changed).IsTrue();

        map.MarkSaved();
        map.SetTile(3, 4, 0x1234);
        AssertThat(map.Changed).IsFalse();

        map.SetTile(3, 4, 0x4321);
        AssertThat(map.ColourAt(3, 4)).IsEqual(0x4321u);
        AssertThat(map.Changed).IsTrue();
    }

    [TestCase(-1, 0)]
    [TestCase(Width, 0)]
    [TestCase(0, Height)]
    public void SetTile_IgnoresTilesOffTheMap(int tileX, int tileY)
    {
        var map = new ExploredMap(Width, Height);

        map.SetTile(tileX, tileY, 0x1111);

        AssertThat(map.Changed).IsFalse();
        AssertThat(map.Revision).IsEqual(0);
    }

    [TestCase]
    public void BlockRevision_ChangesOnlyForTheBlockATileIsIn()
    {
        var map = new ExploredMap();

        map.SetTile(300, 600, 0x1234);

        AssertThat(map.BlockRevision(1, 2)).IsEqual(1);
        AssertThat(map.BlockRevision(1, 1)).IsEqual(0);
        AssertThat(map.BlockRevision(0, 2)).IsEqual(0);
    }

    [TestCase]
    public void BlockRevision_ChangesEverywhereOnALoad()
    {
        var map = new ExploredMap(Width, Height);

        map.Load([]);

        AssertThat(map.BlockRevision(0, 0)).IsEqual(1);
    }

    [TestCase]
    public void Revision_CountsEveryChangeAndEveryLoad()
    {
        var map = new ExploredMap(Width, Height);

        map.SetTile(1, 1, 0x0101);
        AssertThat(map.Revision).IsEqual(1);
        map.SetTile(1, 1, 0x0101);
        AssertThat(map.Revision).IsEqual(1);

        map.Load([]);
        AssertThat(map.Revision).IsEqual(2);
    }

    [TestCase]
    public void ClearBox_WipesOnlyTheBox()
    {
        var map = new ExploredMap(Width, Height);
        map.SetTile(1, 1, 0x0101);
        map.SetTile(2, 2, 0x0202);
        map.SetTile(5, 5, 0x0505);
        map.MarkSaved();

        map.ClearBox(1, 1, 2, 2);

        AssertThat(map.ColourAt(1, 1)).IsEqual(0u);
        AssertThat(map.ColourAt(2, 2)).IsEqual(0u);
        AssertThat(map.ColourAt(5, 5)).IsEqual(0x0505u);
        AssertThat(map.Changed).IsTrue();
    }

    [TestCase]
    public void ClearBox_StaysInsideTheMap()
    {
        var map = new ExploredMap(Width, Height);
        map.SetTile(Width - 1, Height - 1, 0x7777);

        map.ClearBox(Width - 1, Height - 1, 1000, 1000);
        map.ClearBox(-10, -10, 5, 5);

        AssertThat(map.ColourAt(Width - 1, Height - 1)).IsEqual(0u);
    }

    [TestCase]
    public void ClearBox_OfUnexploredTiles_IsNotAChange()
    {
        var map = new ExploredMap(Width, Height);

        map.ClearBox(0, 0, 4, 4);

        AssertThat(map.Changed).IsFalse();
    }

    [TestCase]
    public void Save_WritesTheCurrentClientsHeaderThenZlibTiles()
    {
        var map = new ExploredMap(Width, Height);
        map.SetTile(Width - 1, Height - 1, 0xABCDEF);

        var file = map.Save();

        AssertThat(file[..4]).IsEqual("TLGM"u8.ToArray());
        AssertThat(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(4))).IsEqual(2u);
        AssertThat(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(8))).IsEqual(Width);
        AssertThat(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(12))).IsEqual(Height);
        AssertThat(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(16))).IsEqual((uint)(file.Length - HeaderSize));
        var tiles = Inflate(file[HeaderSize..]);
        AssertThat(tiles.Length).IsEqual(Width * Height * sizeof(uint));
        AssertThat(BinaryPrimitives.ReadUInt32LittleEndian(tiles.AsSpan(tiles.Length - sizeof(uint))))
            .IsEqual(0xABCDEFu);
    }

    [TestCase]
    public void SavedTiles_AreColumnMajor()
    {
        var map = new ExploredMap(Width, Height);
        map.SetTile(1, 0, 0x010000);

        var tiles = Inflate(map.Save()[HeaderSize..]);

        AssertThat(BinaryPrimitives.ReadUInt32LittleEndian(tiles.AsSpan(Height * sizeof(uint)))).IsEqual(0x010000u);
    }

    [TestCase]
    public void ASavedMap_LoadsBackUnchanged()
    {
        var saved = new ExploredMap(Width, Height);
        for (var tileX = 0; tileX < Width; tileX++)
        {
            saved.SetTile(tileX, tileX % Height, (uint)(tileX * 0x010203));
        }

        var loaded = new ExploredMap(Width, Height);
        AssertThat(loaded.Load(saved.Save())).IsTrue();

        for (var tileX = 0; tileX < Width; tileX++)
        {
            AssertThat(loaded.ColourAt(tileX, tileX % Height)).IsEqual(saved.ColourAt(tileX, tileX % Height));
        }

        AssertThat(loaded.Changed).IsFalse();
    }

    [TestCase]
    public void Load_OfAnEmptyOrGarbageFile_GivesABlankMap()
    {
        var map = new ExploredMap(Width, Height);
        map.SetTile(1, 1, 0x0101);

        AssertThat(map.Load([])).IsFalse();
        AssertThat(map.ColourAt(1, 1)).IsEqual(0u);

        map.SetTile(1, 1, 0x0101);
        AssertThat(map.Load("not a map file at all"u8)).IsFalse();
        AssertThat(map.ColourAt(1, 1)).IsEqual(0u);
        AssertThat(map.Changed).IsFalse();
    }

    [TestCase]
    public void Load_OfATruncatedFile_GivesABlankMap()
    {
        var saved = new ExploredMap(Width, Height);
        saved.SetTile(2, 2, 0x0202);
        var file = saved.Save();

        var loaded = new ExploredMap(Width, Height);

        AssertThat(loaded.Load(file[..^4])).IsFalse();
        AssertThat(loaded.ColourAt(2, 2)).IsEqual(0u);
    }

    [TestCase]
    public void Load_OfAMapOfAnotherSize_GivesABlankMap()
    {
        var saved = new ExploredMap(Width, Height);
        saved.SetTile(2, 2, 0x0202);

        var loaded = new ExploredMap(Height, Width);

        AssertThat(loaded.Load(saved.Save())).IsFalse();
        AssertThat(loaded.ColourAt(2, 2)).IsEqual(0u);
    }

    [TestCase]
    public void Load_OfAnUnknownFormat_GivesABlankMap()
    {
        var saved = new ExploredMap(Width, Height);
        saved.SetTile(2, 2, 0x0202);
        var file = saved.Save();
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), 99);

        var loaded = new ExploredMap(Width, Height);

        AssertThat(loaded.Load(file)).IsFalse();
        AssertThat(loaded.ColourAt(2, 2)).IsEqual(0u);
    }

    [TestCase]
    public void Load_ExpandsAnOldRgb555FileToFullColour()
    {
        var packed = new byte[Width * Height * sizeof(ushort)];
        BinaryPrimitives.WriteUInt16LittleEndian(packed.AsSpan(packed.Length - sizeof(ushort)), 31 << 10);
        BinaryPrimitives.WriteUInt16LittleEndian(packed, 0b10000_00000_00001);

        var map = new ExploredMap(Width, Height);

        AssertThat(map.Load(MapFile(1, Width, Height, packed))).IsTrue();
        AssertThat(map.ColourAt(Width - 1, Height - 1)).IsEqual(0xFF0000u);
        AssertThat(map.ColourAt(0, 0)).IsEqual(0x840008u);
    }

    [TestCase]
    public void SaveTo_WritesTheFileAndMarksTheMapSaved()
    {
        var path = Path.Combine(folder, "TLGMap_test_1.dat");
        var map = new ExploredMap(Width, Height);
        map.SetTile(2, 2, 0x0202);

        AssertThat(map.SaveTo(path)).IsTrue();

        AssertThat(map.Changed).IsFalse();
        AssertThat(File.Exists(path + ".tmp")).IsFalse();
        var loaded = new ExploredMap(Width, Height);
        AssertThat(loaded.LoadFrom(path)).IsTrue();
        AssertThat(loaded.ColourAt(2, 2)).IsEqual(0x0202u);
    }

    [TestCase]
    public void SaveTo_ReplacesAnEarlierSave()
    {
        var path = Path.Combine(folder, "TLGMap_test_1.dat");
        var map = new ExploredMap(Width, Height);
        map.SetTile(2, 2, 0x0202);
        map.SaveTo(path);

        map.SetTile(2, 2, 0x0303);
        AssertThat(map.SaveTo(path)).IsTrue();

        var loaded = new ExploredMap(Width, Height);
        loaded.LoadFrom(path);
        AssertThat(loaded.ColourAt(2, 2)).IsEqual(0x0303u);
    }

    [TestCase]
    public void LoadFrom_AMissingFile_GivesABlankMap()
    {
        var map = new ExploredMap(Width, Height);
        map.SetTile(1, 1, 0x0101);

        AssertThat(map.LoadFrom(Path.Combine(folder, "missing.dat"))).IsFalse();

        AssertThat(map.ColourAt(1, 1)).IsEqual(0u);
    }

    [TestCase]
    public void ClearSaved_LeavesABlankMapInPlaceOfTheOldOne()
    {
        var path = Path.Combine(folder, "TLGMap_test_1.dat");
        var explored = new ExploredMap();
        explored.SetTile(1, 1, 0x123456);
        explored.SaveTo(path);
        File.WriteAllText(path + ".tmp", "half written");

        AssertThat(ExploredMap.ClearSaved(path)).IsTrue();

        var reloaded = new ExploredMap();
        AssertThat(reloaded.LoadFrom(path)).IsTrue();
        AssertThat(reloaded.ColourAt(1, 1)).IsEqual(0u);
        AssertThat(File.Exists(path + ".tmp")).IsFalse();
    }

    [TestCase]
    public void ClearSaved_KeepsTheCurrentClientsMapFromBeingImportedAgain()
    {
        var ownPath = Path.Combine(folder, "own", "TLGMap_test_1.dat");
        var sdlPath = Path.Combine(folder, "sdl", "TLGMap_test_1.dat");
        Directory.CreateDirectory(Path.GetDirectoryName(ownPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(sdlPath)!);
        new ExploredMap().SaveTo(sdlPath);

        ExploredMap.ClearSaved(ownPath);

        AssertThat(ServerFiles.PathToLoad(ownPath, sdlPath)).IsEqual(ownPath);
    }

    private static byte[] MapFile(uint format, int width, int height, byte[] tiles)
    {
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, true))
        {
            zlib.Write(tiles);
        }

        var file = new byte[HeaderSize + compressed.Length];
        "TLGM"u8.CopyTo(file);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), format);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(8), (uint)width);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(12), (uint)height);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(16), (uint)compressed.Length);
        compressed.ToArray().CopyTo(file, HeaderSize);
        return file;
    }

    private static byte[] Inflate(byte[] compressed)
    {
        using var zlib = new ZLibStream(new MemoryStream(compressed), CompressionMode.Decompress);
        using var tiles = new MemoryStream();
        zlib.CopyTo(tiles);
        return tiles.ToArray();
    }
}
