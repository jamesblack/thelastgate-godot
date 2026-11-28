using TheLastGate.Net;
using TheLastGate.State;

namespace TheLastGate.Hud.Depot;

public sealed class DepotSwitch
{
    public const double AnswerSeconds = 3;

    private const int DepotFlag = 0x8000;
    private const int DepotNumberMask = 0x7fff;

    private double askedAt;
    private int? askedNumber;
    private int askedPage;

    public static int ShopNumber(int characterNumber) => characterNumber | DepotFlag;

    public byte[] Ask(int characterNumber, int page, double now)
    {
        askedNumber = characterNumber;
        askedPage = page;
        askedAt = now;
        return ClientPackets.OneValue(ClientCommand.Look, ShopNumber(characterNumber));
    }

    public int? Seen(ShopKind? openShop, int shopNumber)
    {
        if (openShop != ShopKind.Depot || (shopNumber & DepotNumberMask) != askedNumber)
        {
            return null;
        }

        askedNumber = null;
        return askedPage;
    }

    public int? Unanswered(double now)
    {
        if (askedNumber is null || now - askedAt < AnswerSeconds)
        {
            return null;
        }

        var unanswered = askedNumber;
        askedNumber = null;
        return unanswered;
    }
}
