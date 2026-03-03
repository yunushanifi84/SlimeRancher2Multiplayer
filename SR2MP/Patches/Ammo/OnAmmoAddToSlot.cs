using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Player;
using SR2MP.Packets.Ammo;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.Ammo
{
    [HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.MaybeAddToSpecificSlot), typeof(IdentifiableType),
        typeof(Identifiable), typeof(int), typeof(int), typeof(bool))]
    public static class OnAmmoAddToSlot
    {
        public static void Postfix(AmmoSlotManager __instance, ref bool __result, IdentifiableType id,
            Identifiable identifiable, int slotIdx, int count, bool overflow)
        {
            if ((!Main.Client.IsConnected && !Main.Server.IsRunning()) || handlingPacket) return;

            if (__result)
            {
                var packet = new AmmoAddToSlotPacket()
                {
                    Identifiable = NetworkActorManager.GetPersistentID(id),
                    SlotIndex = slotIdx,
                    Count = count,
                    ID = __instance.GetPlotID()!
                };

                if (packet.ID == null) return;

                Main.SendToAllOrServer(packet);
            }
        }
    }
}