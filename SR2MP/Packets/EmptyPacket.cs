using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public struct EmptyPacket : IPacket
{
    public PacketType Type { get; init; }
    public PacketReliability Reliability { get; init; }

    public readonly void Serialise(PacketWriter writer) { }

    public readonly void Deserialise(PacketReader reader) { }
}