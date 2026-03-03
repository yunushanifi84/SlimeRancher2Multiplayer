using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Ammo;

[PacketHandler((byte)PacketType.AmmoAddToSlot)]
public sealed class AmmoAddToSlotHandler : BasePacketHandler<AmmoAddToSlotPacket>
{
    protected override bool Handle(AmmoAddToSlotPacket packet, IPEndPoint? _)
    {
        var ammo = NetworkAmmoManager.GetAmmo(packet.ID);

        if (ammo == null) return false;

        handlingPacket = true;
        ammo.MaybeAddToSpecificSlot(actorManager.ActorTypes[packet.Identifiable], null, packet.SlotIndex, packet.Count);
        handlingPacket = false;

        return true;
    }
}