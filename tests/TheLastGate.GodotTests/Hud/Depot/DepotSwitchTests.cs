using GdUnit4;
using TheLastGate.Hud.Depot;
using TheLastGate.Net;
using TheLastGate.State;
using static GdUnit4.Assertions;

namespace TheLastGate.GodotTests.Hud.Depot;

[TestSuite]
public class DepotSwitchTests
{
    private const int CharacterNumber = 1201;
    private const int DepotFlag = 0x8000;
    private const int OtherCharacterNumber = 7;
    private const int FirstCharacterNumber = 5;
    private const int WantedPage = 3;

    private DepotSwitch depotSwitch = new();

    [BeforeTest]
    public void Reset() => depotSwitch = new DepotSwitch();

    [TestCase]
    public void Ask_SendsLookWithTheDepotFlag()
    {
        var packet = depotSwitch.Ask(CharacterNumber, WantedPage, 0);

        AssertThat(packet).IsEqual(ClientPackets.OneValue(ClientCommand.Look, CharacterNumber | DepotFlag));
    }

    [TestCase]
    public void Answer_ClearsTheWait()
    {
        depotSwitch.Ask(CharacterNumber, WantedPage, 0);

        depotSwitch.Seen(ShopKind.Depot, CharacterNumber | DepotFlag);

        AssertThat(depotSwitch.Unanswered(DepotSwitch.AnswerSeconds + 1)).IsNull();
    }

    [TestCase]
    public void Answer_GivesTheWantedPageOnce()
    {
        depotSwitch.Ask(CharacterNumber, WantedPage, 0);

        AssertThat(depotSwitch.Seen(ShopKind.Depot, CharacterNumber | DepotFlag)).IsEqual(WantedPage);
        AssertThat(depotSwitch.Seen(ShopKind.Depot, CharacterNumber | DepotFlag)).IsNull();
    }

    [TestCase]
    public void NoAnswer_ReportsOnceAfterThreeSeconds()
    {
        depotSwitch.Ask(CharacterNumber, WantedPage, 0);

        AssertThat(depotSwitch.Unanswered(DepotSwitch.AnswerSeconds - 0.5)).IsNull();
        AssertThat(depotSwitch.Unanswered(DepotSwitch.AnswerSeconds)).IsEqual(CharacterNumber);
        AssertThat(depotSwitch.Unanswered(DepotSwitch.AnswerSeconds + 1)).IsNull();
    }

    [TestCase]
    public void NewerAsk_ReplacesTheOlder()
    {
        depotSwitch.Ask(FirstCharacterNumber, WantedPage, 0);
        depotSwitch.Ask(OtherCharacterNumber, WantedPage, 2);

        depotSwitch.Seen(ShopKind.Depot, OtherCharacterNumber | DepotFlag);

        AssertThat(depotSwitch.Unanswered(10)).IsNull();
    }

    [TestCase]
    public void AnswerForAnOlderAsk_IsIgnored()
    {
        depotSwitch.Ask(FirstCharacterNumber, WantedPage, 0);
        depotSwitch.Ask(OtherCharacterNumber, WantedPage, 0);

        AssertThat(depotSwitch.Seen(ShopKind.Depot, FirstCharacterNumber | DepotFlag)).IsNull();
        AssertThat(depotSwitch.Unanswered(DepotSwitch.AnswerSeconds)).IsEqual(OtherCharacterNumber);
    }

    [TestCase]
    public void AnotherShopWithTheSameNumber_KeepsTheWait()
    {
        depotSwitch.Ask(CharacterNumber, WantedPage, 0);

        AssertThat(depotSwitch.Seen(ShopKind.Merchant, CharacterNumber | DepotFlag)).IsNull();
        AssertThat(depotSwitch.Unanswered(DepotSwitch.AnswerSeconds)).IsEqual(CharacterNumber);
    }

    [TestCase]
    public void ADepotOfAnotherNumber_KeepsTheWait()
    {
        depotSwitch.Ask(CharacterNumber, WantedPage, 0);

        AssertThat(depotSwitch.Seen(ShopKind.Depot, OtherCharacterNumber | DepotFlag)).IsNull();
        AssertThat(depotSwitch.Unanswered(DepotSwitch.AnswerSeconds)).IsEqual(CharacterNumber);
    }
}
