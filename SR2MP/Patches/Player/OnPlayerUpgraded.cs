using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using SR2MP.Packets.Upgrade;

namespace SR2MP.Patches.Player;

[HarmonyPatch(typeof(UpgradeModel), nameof(UpgradeModel.IncrementUpgradeLevel))]
internal static class OnPlayerUpgraded
{
    // Runs after the local increment, so the model already holds the new absolute level.
    public static void Postfix(UpgradeModel __instance, UpgradeDefinition definition)
    {
        if (HandlingPacket) return;

        if (!Main.Server.IsRunning && !Main.Client.IsConnected) return;

        var packet = new PlayerUpgradePacket
        {
            UpgradeID = (byte)definition._uniqueId,
            // Publish the resulting absolute level (host-authoritative when on the host),
            // not an implicit "+1", so receivers converge to the exact same level.
            Level = (sbyte)__instance.GetUpgradeLevel(definition)
        };

        Main.SendToAllOrServer(packet);
    }
}
