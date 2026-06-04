using SR2MP.Packets.Utils;

namespace SR2MP.Packets.GordoSlime;

internal sealed class GordoSlimeFeedPacket : IPacket
{
    public string ID;

    // Client -> host: a signed change in this gordo's eaten count (normally +1 per feed).
    //   Concurrent feeds from different players sum on the host instead of overwriting.
    // Host -> clients: Authoritative is true and Count is the host's resulting absolute
    //   eaten count, which clients adopt.
    public int Count;
    public bool Authoritative;

    // Needed for unregistered gordos.
    public int RequiredFoodCount;
    public int GordoType;

    public PacketType Type => PacketType.GordoFeed;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;
    public NetworkChannel Channel => NetworkChannel.WorldState;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(ID);
        writer.WritePackedInt(Count);
        writer.WriteBool(Authoritative);
        writer.WritePackedInt(RequiredFoodCount);
        writer.WritePackedInt(GordoType);
    }

    public void Deserialise(PacketReader reader)
    {
        ID = reader.ReadPooledString()!;
        Count = reader.ReadPackedInt();
        Authoritative = reader.ReadBool();
        RequiredFoodCount = reader.ReadPackedInt();
        GordoType = reader.ReadPackedInt();
    }
}
