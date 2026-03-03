using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Loading;

public sealed class ConnectPacket : IPacket
{
    public string PlayerId;
    public string Username;

    public List<ushort> ModHashes;

    public PacketType Type => PacketType.Connect;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public void Serialise(PacketWriter writer)
    {
        writer.WriteString(PlayerId);
        writer.WriteString(Username);

        writer.WriteList(ModHashes, PacketWriterDels.UShort);
    }

    public void Deserialise(PacketReader reader)
    {
        PlayerId = reader.ReadString();
        Username = reader.ReadString();
    
        ModHashes = reader.ReadList(PacketReaderDels.UShort);
    }
}