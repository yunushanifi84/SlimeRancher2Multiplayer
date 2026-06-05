using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Upgrade;

internal struct PlayerUpgradePacket : IPacket
{
    // The upgrade that changed.
    public byte UpgradeID;

    // The absolute level the upgrade is now at (host-authoritative). Sending the absolute
    // level instead of an implicit "+1" makes the packet idempotent: a sender re-applying
    // the echoed authoritative value, or a client that briefly diverged, both converge to
    // the same level. (The old increment model could drift permanently if a peer's base
    // level differed.)
    public sbyte Level;

    public readonly PacketType Type => PacketType.PlayerUpgrade;
    public readonly PacketReliability Reliability => PacketReliability.ReliableOrdered;
    public readonly NetworkChannel Channel => NetworkChannel.WorldState;

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(UpgradeID);
        writer.WriteSByte(Level);
    }

    public void Deserialise(PacketReader reader)
    {
        UpgradeID = reader.ReadByte();
        Level = reader.ReadSByte();
    }
}
