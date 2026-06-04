using System.Net;
using SR2MP.Packets.Utils;

namespace SR2MP.Handlers.Internal;

/// <summary>
/// Base for handlers that make the host the single source of truth for a packet type,
/// using an optimistic-apply + host-reconcile model:
/// <list type="bullet">
/// <item>A client performs an action: its Harmony patch applies it locally (optimistic)
/// and sends the packet to the host. The host applies it and re-broadcasts the
/// authoritative result to <b>everyone, including the original sender</b>, so the sender
/// converges to the host's truth instead of keeping its optimistic value forever.</item>
/// <item>The host performs an action: its patch applies it locally and broadcasts via
/// <c>Server.SendToAll</c> (the normal patch path); clients receive it here and apply it.</item>
/// </list>
/// Subclasses implement <see cref="ApplyLocally"/> (state mutation, automatically wrapped
/// in <see cref="GlobalVariables.HandlingPacket"/> to suppress echo loops) and may override
/// <see cref="BuildAuthoritative"/> to publish the host's real state instead of echoing the
/// client's request verbatim (needed for race-prone counters; harmless absolute/idempotent
/// packets can use the default).
///
/// Packets used with this base should be idempotent (carry absolute values, not deltas), so
/// the sender re-applying the echoed authoritative packet is a no-op. For cumulative/delta
/// semantics see the bespoke <see cref="Currency.CurrencyHandler"/>.
/// </summary>
internal abstract class AuthoritativePacketHandler<T> : BasePacketHandler<T> where T : IPacket, new()
{
    /// <summary>Applies the packet to the local game state. Echo-loop guarded by the caller.</summary>
    protected abstract void ApplyLocally(T packet);

    /// <summary>
    /// Server-side: produces the authoritative packet to broadcast after the host has applied
    /// the client's request. Defaults to echoing the request unchanged, which is correct for
    /// idempotent/absolute packets. Override to publish the host's reconciled state for
    /// race-prone systems.
    /// </summary>
    protected virtual T BuildAuthoritative(T packet) => packet;

    protected sealed override bool Handle(T packet, IPEndPoint? clientEp)
    {
        if (IsServerSide)
        {
            ApplyGuarded(packet);
            Main.Server.SendToAll(BuildAuthoritative(packet));

            // The authoritative packet has now reached everyone (the sender included), so
            // return false to stop BasePacketHandler relaying a second copy.
            return false;
        }

        // Client: this is the host's authoritative packet — adopt it.
        ApplyGuarded(packet);
        return false;
    }

    private void ApplyGuarded(T packet)
    {
        HandlingPacket = true;
        try
        {
            ApplyLocally(packet);
        }
        finally
        {
            HandlingPacket = false;
        }
    }
}
