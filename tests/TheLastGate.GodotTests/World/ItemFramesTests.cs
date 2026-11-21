using GdUnit4;
using TheLastGate.Net;
using TheLastGate.State;
using TheLastGate.World;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.World;

[TestSuite]
public class ItemFramesTests
{
    private const byte AbsoluteSetMap = 0x80;
    private const byte ItemSpriteAndStatus = 0b0001_1000;
    private const int TileIndex = 10 * WorldMap.Size + 10;
    private const int Sprite = 5000;

    private List<string> reports = [];

    [BeforeTest]
    public void ForgetTheReports() => reports = [];

    [TestCase(0)]
    [TestCase(1)]
    public void AStillItem_KeepsItsSprite(int status)
    {
        AssertThat(Frame(status, 1)).IsEqual(Sprite);
        AssertThat(Frame(status, 4)).IsEqual(Sprite);
    }

    [TestCase(2, 0)]
    [TestCase(3, 2)]
    [TestCase(5, 6)]
    [TestCase(6, 0)]
    [TestCase(7, 1)]
    [TestCase(12, 4)]
    [TestCase(18, 2)]
    public void ALoopingItem_StartsOnTheFrameForItsStatus(int status, int frame)
    {
        AssertThat(Frame(status, 1)).IsEqual(Sprite + frame);
    }

    [TestCase]
    public void ALoopingItem_MovesOnEveryOtherTick()
    {
        AssertThat(Frame(2, 1)).IsEqual(Sprite);
        AssertThat(Frame(2, 2)).IsEqual(Sprite + 2);
        AssertThat(Frame(2, 3)).IsEqual(Sprite + 2);
        AssertThat(Frame(2, 4)).IsEqual(Sprite + 4);
    }

    [TestCase(5, 0)]
    [TestCase(7, 0)]
    [TestCase(15, 0)]
    [TestCase(20, 0)]
    public void ALoopingItem_StartsAgainAfterItsLastFrame(int lastStatus, int firstFrame)
    {
        AssertThat(Frame(lastStatus, 2)).IsEqual(Sprite + firstFrame);
    }

    [TestCase]
    public void Status21_PlaysSixtyFourFramesByTheTickCount()
    {
        AssertThat(Frame(21, 1)).IsEqual(Sprite + 1);
        AssertThat(Frame(21, 65)).IsEqual(Sprite + 1);
    }

    [TestCase]
    public void AnUnknownStatus_StaysStillAndIsReported()
    {
        AssertThat(Frame(30, 1)).IsEqual(Sprite);
        AssertThat(reports).HasSize(1);
    }

    [TestCase]
    public void NoItem_HasNoSprite()
    {
        AssertThat(Frame(2, 1, 0)).IsEqual(0);
    }

    private int Frame(int status, int tickCount, int sprite = Sprite)
    {
        var state = new GameState();
        new TickProcessor(state).Process(
            TestCommand.Payload(
                TestCommand.Of(AbsoluteSetMap)
                    .Byte(ItemSpriteAndStatus)
                    .UInt16(TileIndex)
                    .UInt16((ushort)sprite)
                    .Byte((byte)status)));
        return ItemFrames.Sprite(state.Map.Tiles[TileIndex], tickCount, reports.Add);
    }
}
