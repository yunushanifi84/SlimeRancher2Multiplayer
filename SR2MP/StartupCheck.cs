using System.Runtime.InteropServices;
using MelonLoader;
// ReSharper disable InconsistentNaming

namespace SR2MP;

internal static class StartupCheck
{
    private const string RequiredGameVersion = BuildInfo.ExactGameVersion;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    private const uint MB_OK = 0x00000000;
    private const uint MB_ICON_ERROR = 0x00000010;
    private const uint MB_ICON_WARNING = 0x00000030;

    public static void Initialize()
    {
        if (DevMode)
        {
            for (var i = 0; i <= 100; i++)
            {
                SrLogger.LogWarning("DEV BUILD!");
            }
            return;
        }

        var installedGameVersion = MelonLoader.InternalUtils.UnityInformationHandler.GameVersion;

        var versionParts = installedGameVersion.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (versionParts.Length > 0)
        {
            installedGameVersion = versionParts[0];
        }

        // ExactGameVersion is optional (may be null); CompareVersions treats a missing
        // version as "equal", so the strict exact-version gate below is simply skipped.
        switch (CompareVersions(installedGameVersion, RequiredGameVersion))
        {
            case < 0:
                ShowMessageBox(
                    "SR2MP is incompatible with this game version.\n\n" +
                    $"Required: {RequiredGameVersion}\n" +
                    $"Detected: {installedGameVersion}",
                    "SR2MP – Incompatible Game Version",
                    MB_OK | MB_ICON_ERROR, true
                );
                Application.Quit();
                return;
            case > 0:
                ShowMessageBox(
                    "You are running a newer game version than SR2MP was built for.\n\n" +
                    $"Required: {RequiredGameVersion}\n" +
                    $"Detected: {installedGameVersion}\n\n" +
                    "The mod may still work, but issues are possible.",
                    "SR2MP – Newer Game Version Detected",
                    MB_OK | MB_ICON_WARNING, false
                );
                break;
        }

        // Mod-version check is intentionally omitted in this fork: we never fetch
        // latestModVersion.txt from GitHub, so the game won't auto-quit on a version
        // mismatch. The exact-game-version gate above still runs.
    }

    private static void ShowMessageBox(string text, string caption, uint type, bool error)
    {
        try
        {
            MessageBoxW(IntPtr.Zero, text, caption, type);

            if (error)
                SrLogger.LogError($"{caption}\n{text}");
            else
                SrLogger.LogWarning($"{caption}\n{text}");
        }
        catch (Exception ex)
        {
            SrLogger.LogError($"{caption}\n{text}\n{ex}");
        }
    }

    private static int CompareVersions(string version1, string version2)
    {
        // Treat a missing version as "equal" so callers don't have to null-check;
        // an absent constraint should never be the reason startup throws.
        if (string.IsNullOrEmpty(version1) || string.IsNullOrEmpty(version2))
            return 0;

        var v1Parts = version1.Split('.');
        var v2Parts = version2.Split('.');
        var maxLength = Math.Max(v1Parts.Length, v2Parts.Length);

        for (var i = 0; i < maxLength; i++)
        {
            var v1 = i < v1Parts.Length && int.TryParse(v1Parts[i], out var v1Val) ? v1Val : 0;
            var v2 = i < v2Parts.Length && int.TryParse(v2Parts[i], out var v2Val) ? v2Val : 0;

            if (v1 < v2)
                return -1;

            if (v1 > v2)
                return 1;
        }

        return 0;
    }
}