using JetBrains.Annotations;
using MelonLoader;
using SR2E.Utils;

namespace SR2MP.Components.UI;

// TODO: Asset bundle
[RegisterTypeInIl2Cpp(false)]
internal sealed partial class MultiplayerUI : MonoBehaviour
{
    public static MultiplayerUI Instance { get; private set; }

    private bool didUnfocus;

    [UsedImplicitly]
    public void Awake()
    {
        firstTime = Main.SetupUI;
        usernameInput = Main.Username;
        allowCheatsInput = Main.AllowCheats;
        ipInput = Main.SavedConnectIP;
        portInput = Main.SavedConnectPort;
        hostPortInput = Main.SavedHostPort;

        if (Instance)
        {
            SrLogger.LogError("Tried to create instance of MultiplayerUI, but it already exists!");
            Destroy(this);
            return;
        }

        Instance = this;
    }

    [UsedImplicitly]
#pragma warning disable CA1822
    public void OnDestroy() => Instance = null!;
#pragma warning restore CA1822

    [UsedImplicitly]
    public void OnGUI()
    {
        if (Event.current.type == EventType.Layout)
        {
            State = GetState();
            UpdateChatVisibility();
        }

        previousLayoutRect = new Rect(6, 16, WindowWidth, 0);
        previousLayoutHorizontalIndex = 0;

        if (!MenuEUtil.isAnyMenuOpen)
        {
            didUnfocus = false;
            DrawWindow();
            DrawChat();
        }
        else if (!didUnfocus)
        {
            shouldUnfocusChat = true;
            UnfocusChat();
            didUnfocus = true;
        }
    }

    private void DrawWindow()
    {
        if (State == MenuState.Hidden) return;

        GUI.Box(new Rect(6, 6, WindowWidth, WindowHeight), "SR2MP (F4 to toggle)");

        switch (State)
        {
            case MenuState.SettingsInitial:
                FirstTimeScreen();
                break;
            case MenuState.SettingsMain:
                SettingsScreen();
                break;
            case MenuState.DisconnectedMainMenu:
                MainMenuScreen();
                break;
            case MenuState.DisconnectedInGame:
                InGameScreen();
                break;
            case MenuState.ConnectedClient:
                ConnectedScreen();
                break;
            case MenuState.ConnectedHost:
                HostingScreen();
                break;
            case MenuState.Error:
                DrawError();
                break;
            case MenuState.Hidden:
            case MenuState.SettingsHelp:
            case MenuState.Kicked:
            default:
                UnimplementedScreen();
                break;
        }

        AdjustInputValues();
    }
}