namespace SR2MP.Client.Models;

public sealed class RemotePlayer
{
    public readonly string PlayerId;

    public string Username { get; internal set; }

    public Vector3 Position { get; internal set; }
    public float Rotation { get; internal set; }

    // Animation stuff
    public int AirborneState { get; internal set; }
    public bool Moving { get; internal set; }
    public float Yaw { get; internal set; }
    public float HorizontalMovement { get; internal set; }
    public float ForwardMovement { get; internal set; }
    public float HorizontalSpeed { get; internal set; }
    public float ForwardSpeed { get; internal set; }
    public bool Sprinting { get; internal set; }

    public float LookY { get; internal set; }
    public float LastLookY { get; internal set; }

    public RemotePlayer(string playerId) => PlayerId = playerId;
}