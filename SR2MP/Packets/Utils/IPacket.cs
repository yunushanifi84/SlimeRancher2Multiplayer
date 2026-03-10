namespace SR2MP.Packets.Utils;

public interface IPacket : IReliabilityNetObject
{
    PacketType Type { get; }
}