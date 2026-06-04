using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Economy;

internal struct CurrencyPacket : IPacket
{
    // Client -> host: a relative change request (e.g. +120 from a plort sale, -250 from a
    //   purchase). The host applies it to the authoritative balance; concurrent requests
    //   from multiple clients sum instead of overwriting each other.
    // Host -> clients: Authoritative is true and Amount carries the new absolute total,
    //   which every client adopts verbatim. This makes the host the single source of truth.
    public int Amount;
    public byte CurrencyType;
    public bool ShowUINotification;
    public bool Authoritative;

    public readonly PacketType Type => PacketType.CurrencyAdjust;
    public readonly PacketReliability Reliability => PacketReliability.ReliableOrdered;
    public readonly NetworkChannel Channel => NetworkChannel.Economy;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WritePackedInt(Amount);
        writer.WriteByte(CurrencyType);
        writer.WriteBool(ShowUINotification);
        writer.WriteBool(Authoritative);
    }

    public void Deserialise(PacketReader reader)
    {
        Amount = reader.ReadPackedInt();
        CurrencyType = reader.ReadByte();
        ShowUINotification = reader.ReadBool();
        Authoritative = reader.ReadBool();
    }
}
