namespace SR2MP.Packets.Utils;

public interface IReliabilityNetObject : INetObject
{
    PacketReliability Reliability { get; }
}