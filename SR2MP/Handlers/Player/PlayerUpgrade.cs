using SR2MP.Handlers.Internal;
using SR2MP.Packets.Upgrade;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Player;

[PacketHandler((byte)PacketType.PlayerUpgrade)]
internal sealed class PlayerUpgradeHandler : AuthoritativePacketHandler<PlayerUpgradePacket>
{
    // The packet carries the absolute upgrade level, so applying it is idempotent and the
    // host can echo the request verbatim (no BuildAuthoritative override needed): every peer
    // ends up at the same level regardless of how many times the packet is applied.
    protected override void ApplyLocally(PlayerUpgradePacket packet)
    {
        var model = SceneContext.Instance.PlayerState._model.upgradeModel;

        var upgrade = model.upgradeDefinitions.items._items.FirstOrDefault(
            x => x._uniqueId == packet.UpgradeID);

        if (upgrade == null)
            return;

        model.SetUpgradeLevel(upgrade, packet.Level);
    }
}
