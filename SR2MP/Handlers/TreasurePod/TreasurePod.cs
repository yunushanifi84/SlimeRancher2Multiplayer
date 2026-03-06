using System.Net;
using SR2MP.Handlers.Internal;
using SR2MP.Packets.TreasurePod;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.TreasurePod;

[PacketHandler((byte)PacketType.TreasurePod)]
public sealed class TreasurePodHandler : BasePacketHandler<TreasurePodPacket>
{
    protected override bool Handle(TreasurePodPacket packet, IPEndPoint? _)
    {
        var identifier = $"pod{packet.ID}";

        if (!GameState.pods.TryGetValue(identifier, out var model)) return true;
        
        handlingPacket = true;
        model.gameObj?.GetComponent<Il2Cpp.TreasurePod>().Activate();
        handlingPacket = false;
            
        model.state = new ObservableValue<Il2Cpp.TreasurePod.State>(
            Il2Cpp.TreasurePod.State.OPEN
        );
        
        return true;
    }
}