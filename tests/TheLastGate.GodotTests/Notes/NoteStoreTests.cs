using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using GdUnit4;
using TheLastGate.Notes;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Notes;

[TestSuite]
public sealed class NoteStoreTests
{
    private const int HeaderSize = 16;

    private string folder = "";

    [BeforeTest]
    public void MakeAFolder() => folder = Directory.CreateTempSubdirectory("note-store-").FullName;

    [AfterTest]
    public void DeleteTheFolder() => Directory.Delete(folder, true);

    [TestCase]
    public void Save_WritesTheCurrentClientsHeaderThenZlib()
    {
        var file = NoteStore.Save(Sample());

        AssertThat(file[..4]).IsEqual("TLGA"u8.ToArray());
        AssertThat(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(4))).IsEqual(3u);
        var payloadSize = BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(8));
        AssertThat(BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(12))).IsEqual((uint)(file.Length - HeaderSize));
        var payload = Inflate(file[HeaderSize..]);
        AssertThat((uint)payload.Length).IsEqual(payloadSize);
        AssertThat(payload[0]).IsEqual(1);
    }

    [TestCase]
    public void ASavedSet_LoadsBackAsItWas()
    {
        var loaded = NoteStore.Load(NoteStore.Save(Sample()))!;

        AssertThat(loaded.Changed).IsFalse();
        AssertThat(loaded.ShowOnMinimap).IsTrue();
        AssertThat(loaded.Tree().Select(category => category.Name)).ContainsExactly("Keys", "Route");
        var keys = loaded.Tree()[0];
        var route = loaded.Tree()[1];
        AssertThat(keys.Visible).IsTrue();
        AssertThat(keys.ShowLabels).IsFalse();
        AssertThat(route.Visible).IsFalse();
        AssertThat(route.ShowLabels).IsTrue();
        AssertThat(keys.ColourIndex).IsEqual(2);
        AssertThat(route.ColourIndex).IsEqual(5);
        AssertThat(keys.Notes.Select(note => (note.Kind, note.Label, note.TileX, note.TileY)))
            .ContainsExactly((NoteKind.Pin, "Red key", 100, 200), (NoteKind.Text, "Beware", 300, 400));
        AssertThat(route.Notes[0].Label).IsEqual("Maze");
        AssertThat(route.Notes[0].Segments.Count).IsEqual(3);
        AssertThat(route.Notes[0].Segments[2].EndY).IsEqual(90);
    }

    [TestCase]
    public void ShowInWorld_SurvivesSaveAndLoad()
    {
        var loaded = NoteStore.Load(NoteStore.Save(new NoteSet { ShowInWorld = true }))!;

        AssertThat(loaded.ShowInWorld).IsTrue();
        AssertThat(loaded.ShowOnMinimap).IsFalse();
    }

    [TestCase]
    public void BothFlags_SurviveSaveAndLoad()
    {
        var loaded = NoteStore.Load(NoteStore.Save(new NoteSet { ShowOnMinimap = true, ShowInWorld = true }))!;

        AssertThat(loaded.ShowOnMinimap).IsTrue();
        AssertThat(loaded.ShowInWorld).IsTrue();
    }

    [TestCase]
    public void OldFlagsByte_LeavesPathsInWorldOff()
    {
        var file = NoteStore.Save(new NoteSet { ShowOnMinimap = true });
        AssertThat(Inflate(file[HeaderSize..])[0]).IsEqual(1);

        var loaded = NoteStore.Load(file)!;

        AssertThat(loaded.ShowOnMinimap).IsTrue();
        AssertThat(loaded.ShowInWorld).IsFalse();
    }

    [TestCase]
    public void Load_KeepsTheNestingAndCousinsWithTheSameName()
    {
        var saved = new NoteSet { ShowOnMinimap = true };
        var aston = saved.AddCategory("Aston", 1);
        var neiseer = saved.AddCategory("Neiseer", 2);
        saved.AddCategory("Quest Givers", 3, aston);
        var neiseerGivers = saved.AddCategory("Quest Givers", 6, neiseer);
        saved.AddPoint(neiseerGivers, NoteKind.Pin, 55, 66, "Elder");

        var loaded = NoteStore.Load(NoteStore.Save(saved))!;

        AssertThat(loaded.Tree().Select(category => category.Name))
            .ContainsExactly("Aston", "Quest Givers", "Neiseer", "Quest Givers");
        var elder = loaded.Tree()[3];
        AssertThat(elder.ColourIndex).IsEqual(6);
        AssertThat(loaded.FindCategory(elder.ParentId)!.Name).IsEqual("Neiseer");
        AssertThat((elder.Notes[0].TileX, elder.Notes[0].TileY)).IsEqual((55, 66));
    }

    [TestCase("garbage")]
    [TestCase("TLGA")]
    public void Load_OfAnUnreadableFile_GivesNothing(string contents)
    {
        AssertThat(NoteStore.Load(Encoding.ASCII.GetBytes(contents))).IsNull();
    }

    [TestCase]
    public void Load_RefusesBytesLeftOverAfterTheTree()
    {
        var payload = new List<byte> { 0 };
        NoteCodec.EncodeTree(Sample(), 0, payload);
        payload.Add(0);

        AssertThat(NoteStore.Load(StoreFile(3, payload.ToArray()))).IsNull();
    }

    [TestCase]
    public void AFormatOneFile_LoadsWithLabelsShown()
    {
        var source = new NoteSet();
        var old = source.AddCategory("Old", 3);
        source.AddPoint(old, NoteKind.Pin, 12, 34, "Gate");

        var loaded = NoteStore.Load(LegacyFile(1, source))!;

        AssertThat(loaded.Categories).HasSize(1);
        var category = loaded.Categories[0];
        AssertThat(category.Name).IsEqual("Old");
        AssertThat(category.ShowLabels).IsTrue();
        AssertThat(category.Notes).HasSize(1);
        AssertThat(loaded.ShowOnMinimap).IsTrue();
    }

    [TestCase]
    public void AFormatTwoFile_KeepsItsFlagsAndNamesThatClash()
    {
        var source = new NoteSet();
        source.FindCategory(source.AddCategory("Aston", 3))!.ShowLabels = false;
        var lower = source.Attach(NoteCategory.Detached("aston", 4, false, true, []), 0);

        var loaded = NoteStore.Load(LegacyFile(2, source))!;

        AssertThat(loaded.Categories.Select(category => category.Name)).ContainsExactly("Aston", "aston");
        AssertThat(loaded.Categories.Select(category => category.ColourIndex)).ContainsExactly(3, 4);
        AssertThat(loaded.Categories.Select(category => category.Visible)).ContainsExactly(true, false);
        AssertThat(loaded.Categories.Select(category => category.ShowLabels)).ContainsExactly(false, true);
        AssertThat(loaded.Categories.Select(category => category.ParentId).Distinct()).ContainsExactly(0);
        AssertThat(lower).IsNotEqual(0);
    }

    [TestCase]
    public void SaveTo_WritesTheFileAndMarksTheSetSaved()
    {
        var path = Path.Combine(folder, "TLGAnnotations_test_1.dat");
        var set = Sample();

        AssertThat(NoteStore.SaveTo(path, set)).IsTrue();

        AssertThat(set.Changed).IsFalse();
        AssertThat(File.Exists(path + ".tmp")).IsFalse();
        AssertThat(NoteStore.Load(File.ReadAllBytes(path))!.Tree().Select(c => c.Name))
            .ContainsExactly("Keys", "Route");
    }

    [TestCase]
    public void Open_LoadsTheOwnFile()
    {
        var own = Path.Combine(folder, "own.dat");
        NoteStore.SaveTo(own, Sample());

        var (set, setAside) = NoteStore.Open(own, Path.Combine(folder, "sdl.dat"));

        AssertThat(set.Tree().Select(category => category.Name)).ContainsExactly("Keys", "Route");
        AssertThat(setAside).IsFalse();
        AssertThat(set.Changed).IsFalse();
    }

    [TestCase]
    public void Open_ImportsTheCurrentClientsFileWhenThereIsNoOwnFile()
    {
        var sdlClient = Path.Combine(folder, "sdl.dat");
        NoteStore.SaveTo(sdlClient, Sample());

        var (set, _) = NoteStore.Open(Path.Combine(folder, "own.dat"), sdlClient);

        AssertThat(set.Tree().Select(category => category.Name)).ContainsExactly("Keys", "Route");
    }

    [TestCase]
    public void Open_StartsWithMyNotesWhenThereIsNothingToLoad()
    {
        var (set, setAside) = NoteStore.Open(Path.Combine(folder, "own.dat"), Path.Combine(folder, "sdl.dat"));

        AssertThat(set.Categories).HasSize(1);
        var category = set.Categories[0];
        AssertThat(category.Name).IsEqual("My notes");
        AssertThat(category.ColourIndex).IsEqual(1);
        AssertThat(set.Changed).IsFalse();
        AssertThat(setAside).IsFalse();
    }

    [TestCase]
    public void Open_SetsADamagedOwnFileAsideReplacingAnOlderOne()
    {
        var own = Path.Combine(folder, "own.dat");
        File.WriteAllText(own + ".bad", "older");
        File.WriteAllText(own, "garbage");

        var (set, setAside) = NoteStore.Open(own, Path.Combine(folder, "sdl.dat"));

        AssertThat(setAside).IsTrue();
        AssertThat(File.Exists(own)).IsFalse();
        AssertThat(File.ReadAllText(own + ".bad")).IsEqual("garbage");
        AssertThat(set.Categories).HasSize(1);
        AssertThat(set.Categories[0].Name).IsEqual("My notes");
    }

    [TestCase]
    public void Open_NeverMovesTheCurrentClientsFile()
    {
        var sdlClient = Path.Combine(folder, "sdl.dat");
        File.WriteAllText(sdlClient, "garbage");

        var (set, setAside) = NoteStore.Open(Path.Combine(folder, "own.dat"), sdlClient);

        AssertThat(setAside).IsFalse();
        AssertThat(File.ReadAllText(sdlClient)).IsEqual("garbage");
        AssertThat(File.Exists(sdlClient + ".bad")).IsFalse();
        AssertThat(set.Categories).HasSize(1);
        AssertThat(set.Categories[0].Name).IsEqual("My notes");
    }

    private static NoteSet Sample()
    {
        var set = new NoteSet { ShowOnMinimap = true };
        var keys = set.AddCategory("Keys", 2);
        var route = set.AddCategory("Route", 5);
        set.AddPoint(keys, NoteKind.Pin, 100, 200, "Red key");
        set.AddPoint(keys, NoteKind.Text, 300, 400, "Beware");
        set.AddPath(
            route,
            [new PathSegment(10, 20, 30, 40), new PathSegment(30, 40, 50, 60), new PathSegment(50, 60, 70, 90)],
            "Maze");
        set.FindCategory(keys)!.ShowLabels = false;
        set.FindCategory(route)!.Visible = false;
        return set;
    }

    private static byte[] LegacyFile(uint format, NoteSet source)
    {
        var payload = new List<byte> { 1 };
        NoteCodec.WriteVarint(payload, (uint)source.Categories.Count);
        foreach (var category in source.Categories)
        {
            payload.Add(category.Visible ? (byte)1 : (byte)0);
            if (format >= 2)
            {
                payload.Add(category.ShowLabels ? (byte)1 : (byte)0);
            }

            NoteCodec.EncodeCategory(category, payload);
        }

        return StoreFile(format, payload.ToArray());
    }

    private static byte[] StoreFile(uint format, byte[] payload)
    {
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, true))
        {
            zlib.Write(payload);
        }

        var file = new byte[HeaderSize + compressed.Length];
        "TLGA"u8.CopyTo(file);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), format);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(8), (uint)payload.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(12), (uint)compressed.Length);
        compressed.ToArray().CopyTo(file, HeaderSize);
        return file;
    }

    private static byte[] Inflate(byte[] compressed)
    {
        using var zlib = new ZLibStream(new MemoryStream(compressed), CompressionMode.Decompress);
        using var payload = new MemoryStream();
        zlib.CopyTo(payload);
        return payload.ToArray();
    }
}
