using GdUnit4;
using TheLastGate.Map;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Map;

[TestSuite]
public class ExploredMapRecorderTests
{
    private const int Floor = 1100;
    private const int Item = 2200;

    private ExploredMap map = new(64, 64);
    private ExploredMapRecorder recorder = new(new ExploredMap(64, 64));

    [BeforeTest]
    public void StartRecordingABlankMap()
    {
        map = new ExploredMap(64, 64);
        recorder = new ExploredMapRecorder(map);
    }

    [TestCase]
    public void SpriteToAverage_PrefersTheItemOverTheFloor()
    {
        AssertThat(ExploredMapRecorder.SpriteToAverage(0, Floor, Item)).IsEqual(Item);
        AssertThat(ExploredMapRecorder.SpriteToAverage(0, Floor, 0)).IsEqual(Floor);
    }

    [TestCase]
    public void SpriteToAverage_SkipsTilesThePlayerCanNotSee()
    {
        AssertThat(ExploredMapRecorder.SpriteToAverage((uint)MapFlags.Invisible, Floor, Item)).IsNull();
    }

    [TestCase(0)]
    [TestCase(ExploredMapRecorder.EmptyFloorSprite)]
    public void SpriteToAverage_SkipsUnlitAndOffMapTiles(int floor)
    {
        AssertThat(ExploredMapRecorder.SpriteToAverage(0, floor, Item)).IsNull();
    }

    [TestCase]
    public void See_RecordsTheColourAtTheTilesWorldPosition()
    {
        recorder.See(12, 34, 0x445566);

        AssertThat(map.ColourAt(12, 34)).IsEqual(0x445566u);
    }

    [TestCase]
    public void ClearBox_UnexploresTheBox()
    {
        recorder.See(5, 5, 0x0505);

        recorder.ClearBox(new MinimapBox(4, 4, 2, 2));

        AssertThat(map.ColourAt(5, 5)).IsEqual(0u);
    }

    [TestCase]
    public void WhileSpectating_NothingIsRecordedOrCleared()
    {
        recorder.See(5, 5, 0x0505);
        recorder.Paused = true;

        recorder.See(6, 6, 0x0606);
        recorder.ClearBox(new MinimapBox(4, 4, 2, 2));

        AssertThat(map.ColourAt(6, 6)).IsEqual(0u);
        AssertThat(map.ColourAt(5, 5)).IsEqual(0x0505u);
    }

    [TestCase]
    public void Unpausing_ResumesRecording()
    {
        recorder.Paused = true;

        recorder.Paused = false;
        recorder.See(6, 6, 0x0606);

        AssertThat(map.ColourAt(6, 6)).IsEqual(0x0606u);
    }
}
