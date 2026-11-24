using System.Diagnostics;
using GdUnit4;
using Godot;
using TheLastGate.Assets;
using TheLastGate.Profiling;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Profiling;

[TestSuite]
[RequireGodotRuntime]
public class TimingsViewTests
{
    private const long SpriteCacheBytes = 16L * 1024 * 1024;
    private const string Section = "test.section";
    private const string Monitor = "Timings/" + Section;
    private const string PacketMonitor = "Packets/TestSender";

    private PacketCounter packets = null!;
    private TimingRecorder recorder = null!;
    private SpriteLoader sprites = null!;
    private TimingsView view = null!;

    private static SceneTree Tree => (SceneTree)Engine.GetMainLoop();

    [BeforeTest]
    public void ShowTheView()
    {
        var dataFolder = Path.GetFullPath(Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", "..", "data"));
        sprites = new SpriteLoader(dataFolder, SpriteCacheBytes);
        recorder = new TimingRecorder();
        packets = new PacketCounter();
        view = GD.Load<PackedScene>("res://Profiling/timings_view.tscn").Instantiate<TimingsView>();
        Tree.Root.AddChild(view);
        view.Watch(recorder, packets, null, new ClassicFonts());
    }

    [AfterTest]
    public void CloseTheView()
    {
        if (GodotObject.IsInstanceValid(view))
        {
            view.Free();
        }

        sprites.Dispose();
    }

    [TestCase]
    public void TheView_StartsHidden() => AssertThat(view.Visible).IsFalse();

    [TestCase]
    public async Task ASection_GetsOneMonitor()
    {
        recorder.Record(Section, 1);
        await Frames(3);

        AssertThat(Performance.HasCustomMonitor(Monitor)).IsTrue();
    }

    [TestCase]
    public async Task FreeingTheView_RemovesItsMonitors()
    {
        recorder.Record(Section, 1);
        await Frames(3);
        view.Free();

        AssertThat(Performance.HasCustomMonitor(Monitor)).IsFalse();
    }

    [TestCase]
    public async Task AVisibleView_ShowsARowPerSection()
    {
        view.Visible = true;
        recorder.Record(Section, 1);
        await Frames(3);

        var texts = view.GetNode("%Sections").GetChildren().OfType<Label>().Select(label => label.Text);
        AssertThat(texts).Contains(Section);
    }

    [TestCase]
    public async Task ASender_GetsOnePacketMonitorThatGoesWithTheView()
    {
        packets.Count("TestSender", new byte[16], Stopwatch.GetTimestamp());
        await Frames(3);
        AssertThat(Performance.HasCustomMonitor(PacketMonitor)).IsTrue();

        view.Free();
        AssertThat(Performance.HasCustomMonitor(PacketMonitor)).IsFalse();
    }

    [TestCase]
    public async Task AVisibleView_ShowsARowPerSenderAndType()
    {
        view.Visible = true;
        packets.Count("TestSender", new byte[16], Stopwatch.GetTimestamp());
        await Frames(3);

        var texts = view.GetNode("%Packets").GetChildren().OfType<Label>().Select(label => label.Text);
        AssertThat(texts).Contains("TestSender");
    }

    [TestCase]
    public async Task TheWholeFrame_GetsAMonitor()
    {
        await Frames(3);

        AssertThat(Performance.HasCustomMonitor("Timings/frame")).IsTrue();
    }

    [TestCase]
    public async Task AVisibleView_ShowsTheWholeFrameFirstAndTimesItsOwnRefresh()
    {
        view.Visible = true;
        await Frames(3);

        var texts = view.GetNode("%Sections").GetChildren().OfType<Label>().Select(label => label.Text).ToList();
        AssertThat(texts[6]).IsEqual("frame");
        AssertThat(recorder.Snapshot().Select(row => row.Name)).Contains("profiling.overlay");
    }

    private static async Task Frames(int count)
    {
        for (var frame = 0; frame < count; frame++)
        {
            await Tree.ToSignal(Tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
