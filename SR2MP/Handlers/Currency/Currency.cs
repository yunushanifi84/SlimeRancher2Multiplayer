using System.Net;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Economy;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Currency;

[PacketHandler((byte)PacketType.CurrencyAdjust)]
internal sealed class CurrencyHandler : BasePacketHandler<CurrencyPacket>
{
    protected override bool Handle(CurrencyPacket packet, IPEndPoint? _)
    {
        var currencyDefinition = GameContext.Instance.LookupDirector
            ._currencyList._currencies[packet.CurrencyType - 1]!.Cast<ICurrency>();

        var playerState = SceneContext.Instance.PlayerState;

        if (packet.Authoritative)
        {
            // Host -> client: adopt the host's absolute total verbatim. This is the
            // single source of truth; any local drift is corrected here.
            ApplyDelta(playerState, currencyDefinition,
                packet.Amount - playerState.GetCurrency(currencyDefinition),
                packet.ShowUINotification);

            // A client never re-broadcasts; this is already the authoritative value.
            return false;
        }

        // Host applying a client's relative request: add the delta to the authoritative
        // balance, then broadcast the resulting absolute total to everyone (below).
        if (!Main.Server.IsRunning)
            return false;

        ApplyDelta(playerState, currencyDefinition, packet.Amount, packet.ShowUINotification);

        var authoritative = new CurrencyPacket
        {
            Amount = playerState.GetCurrency(currencyDefinition),
            CurrencyType = packet.CurrencyType,
            ShowUINotification = packet.ShowUINotification,
            Authoritative = true
        };

        // Send the corrected absolute total to ALL clients (including the requester, so
        // it converges away from its optimistic local value). We send explicitly rather
        // than returning true, because the relayed packet must carry the authoritative
        // total, not the original delta request.
        Main.Server.SendToAll(authoritative);
        return false;
    }

    private static void ApplyDelta(PlayerState playerState, ICurrency currency, int delta, bool showUi)
    {
        if (delta == 0)
            return;

        HandlingPacket = true;

        if (delta < 0)
            playerState.SpendCurrency(currency, -delta);
        else
            playerState.AddCurrency(currency, delta, showUi);

        HandlingPacket = false;
    }
}
