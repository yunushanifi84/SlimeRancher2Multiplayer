using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Ammo;

public class AmmoDecrementPacket : IPacket
{
    public int SlotIndex;
    public int Count;
    public string ID;
    
    public PacketType Type => PacketType.AmmoDecrement;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteInt(SlotIndex);
        writer.WriteInt(Count);
        writer.WriteString(ID);
    }

    public void Deserialise(PacketReader reader)
    {
        SlotIndex = reader.ReadInt();
        Count = reader.ReadInt();
        ID = reader.ReadString();
    }
}