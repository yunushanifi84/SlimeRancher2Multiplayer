using HarmonyLib;
using SR2MP.Packets.FX;
using SR2MP.Packets.GordoSlime;
using SR2MP.Shared.Managers;

namespace SR2MP.Patches.GordoSlime;

[HarmonyPatch(typeof(GordoEat), nameof(GordoEat.DoEat))]
internal static class OnGordoFed
{
    // Capture the eaten count before the feed so the postfix can send a delta (the host
    // needs the signed change, not the absolute total, to avoid concurrent-feed overwrites).
    public static void Prefix(GordoEat __instance, out int __state)
        => __state = __instance.GordoModel.GordoEatenCount;

    public static void Postfix(GordoEat __instance, int __state)
    {
        if (HandlingPacket)
            return;

        var model = __instance.GordoModel;
        var delta = model.GordoEatenCount - __state;
        if (delta == 0)
            return;

        var gordoType = NetworkActorManager.GetPersistentID(model.identifiableType);

        if (Main.Server.IsRunning)
        {
            // Host is authoritative: publish the resulting absolute eaten count.
            Main.Server.SendToAll(new GordoSlimeFeedPacket
            {
                ID = __instance.Id,
                Count = model.GordoEatenCount,
                Authoritative = true,
                RequiredFoodCount = model.targetCount,
                GordoType = gordoType
            });
        }
        else if (Main.Client.IsConnected)
        {
            // Client applied optimistically; send the delta as a request.
            Main.Client.SendPacket(new GordoSlimeFeedPacket
            {
                ID = __instance.Id,
                Count = delta,
                Authoritative = false,
                RequiredFoodCount = model.targetCount,
                GordoType = gordoType
            });
        }

        var soundPacket = new WorldFXPacket
        {
            Position = __instance.transform.position,
            FX = WorldFXType.GordoFoodEatenSound
        };
        Main.SendToAllOrServer(soundPacket);
    }
}
