using System.Net;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.GordoSlime;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.GordoSlime;

[PacketHandler((byte)PacketType.GordoFeed)]
internal sealed class GordoSlimeFeedHandler : BasePacketHandler<GordoSlimeFeedPacket>
{
    protected override bool Handle(GordoSlimeFeedPacket packet, IPEndPoint? _)
    {
        if (packet.Authoritative)
        {
            // Host -> client: adopt the host's absolute eaten count verbatim.
            SetEatenCount(packet, packet.Count);
            return false;
        }

        // Host applying a client's delta request: add it to the authoritative count, then
        // broadcast the resulting absolute total to everyone (sender included).
        if (!Main.Server.IsRunning)
            return false;

        var current = GameState.gordos.TryGetValue(packet.ID, out var gordo)
            ? gordo.GordoEatenCount
            : 0;
        var newCount = current + packet.Count;

        SetEatenCount(packet, newCount);

        Main.Server.SendToAll(new GordoSlimeFeedPacket
        {
            ID = packet.ID,
            Count = newCount,
            Authoritative = true,
            RequiredFoodCount = packet.RequiredFoodCount,
            GordoType = packet.GordoType
        });

        return false;
    }

    private static void SetEatenCount(GordoSlimeFeedPacket packet, int eatenCount)
    {
        if (GameState.gordos.TryGetValue(packet.ID, out var gordo))
        {
            gordo.GordoEatenCount = eatenCount;
        }
        else
        {
            gordo = new GordoModel
            {
                fashions = new CppCollections.List<IdentifiableType>(0),
                gordoEatCount = eatenCount,
                gordoSeen = false,
                gameObj = null,
                targetCount = packet.RequiredFoodCount,
                identifiableType = ActorManager.ActorTypes[packet.GordoType]
            };

            GameState.gordos.Add(packet.ID, gordo);
        }
    }
}
