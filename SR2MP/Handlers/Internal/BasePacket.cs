using System.Net;
using System.Runtime.CompilerServices;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Internal;

internal abstract class BasePacketHandler<T> : IClientPacketHandler, IServerPacketHandler where T : IPacket, new()
{
    public bool IsServerSide { protected get; set; }

    // Do NOT override these! Only the ApiHandler class should be dealing with this!
    public virtual void Handle(PacketReader reader)
    {
        if (!IsServerSide)
            ProcessPacket(reader, null);
    }

    public virtual void Handle(PacketReader reader, IPEndPoint? clientEp)
    {
        if (IsServerSide)
            ProcessPacket(reader, clientEp);
    }

    private void ProcessPacket(PacketReader reader, IPEndPoint? clientEp)
    {
        var packet = reader.ReadPacket<T>();

        // Server-authoritative gate. A handler that opts into authority by overriding
        // Validate can reject a client request: the change is neither applied locally
        // nor relayed, and the originating client is sent a corrective packet so it
        // converges back to the host's truth.
        if (IsServerSide && !Validate(packet, clientEp))
        {
            SendCorrection(packet, clientEp);
            return;
        }

        var shouldSend = Handle(packet, clientEp);

        if (IsServerSide && shouldSend)
            PacketSender.SendToAllExcept(packet, clientEp);
    }

    protected abstract bool Handle(T packet, IPEndPoint? clientEp);

    /// <summary>
    /// Server-side authority check, run before <see cref="Handle"/>. Defaults to
    /// permissive so existing handlers keep relaying unchanged. Override to make the
    /// host the source of truth for this packet type: return <c>false</c> to reject a
    /// client's request.
    /// </summary>
    protected virtual bool Validate(T packet, IPEndPoint? clientEp) => true;

    /// <summary>
    /// Sends a corrective packet back to a client whose request <see cref="Validate"/>
    /// rejected, so it re-converges to the authoritative state. No-op by default.
    /// </summary>
    protected virtual void SendCorrection(T packet, IPEndPoint? clientEp) { }
}

internal static class PacketSender
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendPacket<T>(T packet) where T : IPacket
        => Main.Client.SendPacket(packet);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendToAllExcept<T>(T packet, IPEndPoint? excludedEp) where T : IPacket
        => Main.Server.SendToAllExcept(packet, excludedEp);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SendToClient<T>(T packet, IPEndPoint endPoint) where T : IPacket
        => Main.Server.SendToClient(packet, endPoint);
}