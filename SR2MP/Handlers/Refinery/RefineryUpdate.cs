using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.Utils;
using SR2MP.Packets.World;

namespace SR2MP.Handlers.Refinery;

[PacketHandler((byte)PacketType.RefineryUpdate)]
internal sealed class RefineryUpdateHandler : BasePacketHandler<RefineryUpdatePacket>
{
    protected override bool Handle(RefineryUpdatePacket packet, IPEndPoint? _)
    {
        if (!ActorManager.ActorTypes.TryGetValue(packet.ItemID, out var identType))
            return false;

        var model = SceneContext.Instance.GadgetDirector._model;

        if (packet.Authoritative)
        {
            // Host -> client: adopt the host's absolute count verbatim.
            SetCount(model, identType, packet.Count);
            return false;
        }

        // Host applying a client's delta request: add it to the authoritative count, then
        // broadcast the resulting absolute total to everyone (sender included).
        if (!Main.Server.IsRunning)
            return false;

        var current = model._itemCounts.TryGetValue(identType, out var c) ? c : 0;
        var newCount = current + packet.Count;

        SetCount(model, identType, newCount);

        Main.Server.SendToAll(new RefineryUpdatePacket
        {
            Count = newCount,
            ItemID = packet.ItemID,
            Authoritative = true
        });

        return false;
    }

    private static void SetCount(Il2CppMonomiPark.SlimeRancher.DataModel.GadgetsModel model,
        IdentifiableType identType, int count)
    {
        HandlingPacket = true;
        model.SetCount(identType, count);
        HandlingPacket = false;
    }
}
