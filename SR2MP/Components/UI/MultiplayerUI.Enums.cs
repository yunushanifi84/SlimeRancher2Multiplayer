namespace SR2MP.Components.UI;

internal sealed partial class MultiplayerUI
{
    public enum MenuState : byte
    {
        Hidden,
        DisconnectedMainMenu,
        DisconnectedInGame,
        ConnectedClient,
        ConnectedHost,
        SettingsInitial,
        SettingsMain,
        SettingsHelp,
        Kicked,
        Error
    }

    public enum ErrorType : byte
    {
        None,
        ConnectionDeny,
        // Kick,
    }

    // public enum HelpTopic : byte
    // {
    //     None,
    //     Root,
    //     PlayIt,
    //     SyncState,
    //     DiscordSupport
    // }
}