using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public struct ApiPacket<T> : IPacket where T : IReliabilityNetObject, new()
{
    public readonly PacketType Type => PacketType.ApiCall;
    public readonly PacketReliability Reliability => Data.Reliability;

    public T Data;

    public void Deserialise(PacketReader reader) => Data = reader.ReadNetObject<T>();

    public readonly void Serialise(PacketWriter writer) => writer.WriteNetObject(Data);
}