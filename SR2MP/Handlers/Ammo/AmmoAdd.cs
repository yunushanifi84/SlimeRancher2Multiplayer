using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Ammo;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;

namespace SR2MP.Handlers.Ammo;

[PacketHandler((byte)PacketType.AmmoAdd)]
public sealed class AmmoAddHandler : BasePacketHandler<AmmoAddPacket>
{
    protected override bool Handle(AmmoAddPacket packet, IPEndPoint? _)
    {
        var ammo = NetworkAmmoManager.GetAmmo(packet.ID);
        
        if (ammo == null) return false;
        var ident = actorManager.ActorTypes[packet.Identifiable];
        handlingPacket = true;
        ammo.MaybeAddToSpecificSlot(ident, null, ammo.GetNextSlot(ident));
        handlingPacket = false;
        
        return true;
    }
}