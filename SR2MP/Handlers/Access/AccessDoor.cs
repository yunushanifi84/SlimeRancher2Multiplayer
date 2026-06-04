using Il2CppMonomiPark.World;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Handlers.Access;

[PacketHandler((byte)PacketType.AccessDoor)]
internal sealed class AccessDoorHandler : AuthoritativePacketHandler<AccessDoorPacket>
{
    // Door state is absolute (OPEN/LOCKED/...), so concurrent opens converge to the same
    // value and the request can be echoed as-is — no BuildAuthoritative override needed.
    protected override void ApplyLocally(AccessDoorPacket packet)
    {
        var model = GameState.doors[packet.ID];
        model.gameObj.GetComponent<AccessDoor>().CurrState = packet.State;
    }
}
