namespace DoomedCLI.Utility;

public sealed class UserSession
{
    private static readonly Lazy<UserSession> _instance = new(() => new UserSession());

    public static UserSession Instance => _instance.Value;

    public string Username { get; set; } = string.Empty;

    private UserSession()
    {
    }
}
