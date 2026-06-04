using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.Economy;
using SR2MP.Packets.Economy;

namespace SR2MP.Patches.Economy;

[HarmonyPatch(typeof(PlayerState))]
internal static class CurrencyPatch
{
    [HarmonyPostfix, HarmonyPatch(nameof(PlayerState.AddCurrency))]
    public static void AddCurrency(
        PlayerState __instance,
        ICurrency currencyDefinition,
        int __1,
        bool showUiNotification)
    {
        if (HandlingPacket) return;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (currencyDefinition == null || __1 == 0)
            return;

        Send(__instance, currencyDefinition, __1, showUiNotification);
    }

    [HarmonyPostfix, HarmonyPatch(nameof(PlayerState.SpendCurrency))]
    public static void SpendCurrency(
        PlayerState __instance,
        ICurrency currency,
        int __1)
    {
        if (HandlingPacket) return;

        if (__1 == 0)
            return;

        Send(__instance, currency, -__1, showUiNotification: true);
    }

    // The local change has already been applied to __instance by the time this postfix runs.
    private static void Send(PlayerState instance, ICurrency currency, int delta, bool showUiNotification)
    {
        var currencyType = (byte)currency.PersistenceId;

        if (Main.Server.IsRunning)
        {
            // Host is authoritative: broadcast the resulting absolute total so every
            // client adopts it. Concurrent host/client changes can't overwrite — the
            // host's balance is the truth and is published as-is.
            Main.Server.SendToAll(new CurrencyPacket
            {
                Amount = instance._model.GetCurrencyAmount(currency),
                CurrencyType = currencyType,
                ShowUINotification = showUiNotification,
                Authoritative = true
            });
        }
        else if (Main.Client.IsConnected)
        {
            // Client applied optimistically; send the delta as a request. The host sums
            // it into the authoritative balance and echoes back the absolute total, which
            // this client then reconciles against.
            Main.Client.SendPacket(new CurrencyPacket
            {
                Amount = delta,
                CurrencyType = currencyType,
                ShowUINotification = showUiNotification,
                Authoritative = false
            });
        }
    }
}
