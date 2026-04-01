namespace DoomedCLI.Utility;

public sealed class UserSession
{
    private static readonly Lazy<UserSession> _instance = new(() => new UserSession());

    public static UserSession Instance => _instance.Value;

    public string Username { get; set; } = string.Empty;
    public int UserId { get; set; } = -1;
    public int LobbyId { get; set; } = -1;

    private UserSession()
    {
    }
}
