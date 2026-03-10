using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Actor;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Actor;

[PacketHandler((byte)PacketType.ActorDestroy)]
public sealed class ActorDestroyHandler : BasePacketHandler<ActorDestroyPacket>
{
    protected override bool Handle(ActorDestroyPacket packet, IPEndPoint? _)
    {
        //if (!actorManager.Actors.TryGetValue(packet.ActorId.Value, out var actor))
        //{
        //    SrLogger.LogDebug($"Actor {packet.ActorId.Value} doesn't exist (already destroyed?)", SrLogTarget.Both);
        //    return false;
        //}

        if (!GameState.TryGetIdentifiableModel(packet.ActorId, out var actor))
        {
            // SrLogger.LogError($"Tried to destroy actor that doesn't exist!\n\tID: {packet.ActorId}", SrLogTarget.Both);
            return false;
        }
        
        GameState.identifiables.Remove(packet.ActorId);
        GameState.identifiablesByIdent[actor.ident].Remove(actor);
        GameState.DestroyIdentifiableModel(actor);
        actorManager.Actors.Remove(actor.actorId.Value);

        var obj = actor.GetGameObject();
        handlingPacket = true;

        if (obj)
            Destroyer.DestroyAny(actor.GetGameObject(), "SR2MP.ActorDestroyHandler");

        handlingPacket = false;
        return true;
    }
}