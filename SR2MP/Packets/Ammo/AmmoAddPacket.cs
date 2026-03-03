using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Ammo;

public class AmmoAddPacket : IPacket
{
    public int Identifiable;
    public int Count;
    public string ID;
    
    public PacketType Type => PacketType.AmmoAdd;
    public PacketReliability Reliability => PacketReliability.Reliable;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteInt(Identifiable);
        writer.WriteInt(Count);
        writer.WriteString(ID);
    }

    public void Deserialise(PacketReader reader)
    {
        Identifiable = reader.ReadInt();
        Count = reader.ReadInt();
        ID = reader.ReadString();
    }
}