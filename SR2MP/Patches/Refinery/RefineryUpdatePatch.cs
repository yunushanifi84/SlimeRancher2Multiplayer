using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.World;

namespace SR2MP.Patches.Refinery;

[HarmonyPatch(typeof(GadgetsModel), nameof(GadgetsModel.SetCount))]
internal static class RefineryUpdate
{
    // Capture the pre-change count so the postfix can compute a delta (the host needs the
    // signed change, not the absolute new value, to avoid concurrent-deposit overwrites).
    public static void Prefix(GadgetsModel __instance, IdentifiableType type, out int __state)
        => __state = __instance._itemCounts.TryGetValue(type, out var current) ? current : 0;

    public static void Postfix(GadgetsModel __instance, IdentifiableType type, int newCount, int __state)
    {
        if (HandlingPacket)
            return;

        var delta = newCount - __state;
        if (delta == 0)
            return;

        var itemId = (ushort)GameContext.Instance.AutoSaveDirector._saveReferenceTranslation
            ._identifiableTypeToPersistenceId.GetPersistenceId(type);

        if (Main.Server.IsRunning)
        {
            // Host is authoritative: publish the resulting absolute count to everyone.
            Main.Server.SendToAll(new RefineryUpdatePacket
            {
                Count = newCount,
                ItemID = itemId,
                Authoritative = true
            });
        }
        else if (Main.Client.IsConnected)
        {
            // Client applied optimistically; request the delta from the host, which sums it
            // into the authoritative count and echoes back the absolute total.
            Main.Client.SendPacket(new RefineryUpdatePacket
            {
                Count = delta,
                ItemID = itemId,
                Authoritative = false
            });
        }
    }
}
