using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Ammo;

public class AmmoAddToSlotPacket : IPacket
{
    public int Identifiable;
    public int SlotIndex;
    public int Count;
    public string ID;
    
    public PacketType Type => PacketType.AmmoAddToSlot;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteInt(Identifiable);
        writer.WriteInt(SlotIndex);
        writer.WriteInt(Count);
        writer.WriteString(ID);
    }

    public void Deserialise(PacketReader reader)
    {
        Identifiable = reader.ReadInt();
        SlotIndex = reader.ReadInt();
        Count = reader.ReadInt();
        ID = reader.ReadString();
    }
}