using SR2MP.Packets.Utils;

namespace SR2MP.Packets.World;

internal sealed class RefineryUpdatePacket : IPacket
{
    // Client -> host: a signed change in the refinery count for ItemID (e.g. +3 deposited,
    //   -1 withdrawn). Concurrent changes from different players sum on the host instead of
    //   overwriting each other.
    // Host -> clients: Authoritative is true and Count carries the host's resulting absolute
    //   count, which every client adopts.
    public int Count;
    public ushort ItemID;
    public bool Authoritative;

    public PacketType Type => PacketType.RefineryUpdate;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;
    public NetworkChannel Channel => NetworkChannel.Ammo;

    public void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(Count);
        writer.WriteUShort(ItemID);
        writer.WriteBool(Authoritative);
    }

    public void Deserialise(PacketReader reader)
    {
        Count = reader.ReadPackedInt();
        ItemID = reader.ReadUShort();
        Authoritative = reader.ReadBool();
    }
}
