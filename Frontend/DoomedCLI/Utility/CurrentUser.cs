using DoomedCLI.Utility.HTTP;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoomedCLI.Utility;

public static class CurrentUser
{
    private sealed record MeResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("username")] string Username,
        [property: JsonPropertyName("lobbyId")] int LobbyId);

    public static void SetUsername(string username)
    {
        UserSession.Instance.Username = username;
    }

    public static async Task<string> GetUsernameAsync(CancellationToken cancellationToken = default, bool refresh = false)
    {
        await GetUserInfoAsync(cancellationToken, refresh).ConfigureAwait(false);
        return UserSession.Instance.Username;
    }

    public static async Task<(int userId, string username, int lobbyId)> GetUserInfoAsync(CancellationToken cancellationToken = default, bool refresh = false)
    {
        var session = UserSession.Instance;
        if (!refresh && !string.IsNullOrWhiteSpace(session.Username) && session.UserId != -1)
        {
            return (session.UserId, session.Username, session.LobbyId);
        }

        var host = Environment.GetEnvironmentVariable("HOST_IP") ?? string.Empty;
        var httpHandler = new HttpHandler(new HttpClient
        {
            BaseAddress = new Uri(host)
        });

        var result = await httpHandler.GetAsync("api/users/me", cancellationToken).ConfigureAwait(false);
        var response = JsonSerializer.Deserialize<MeResponse>(result)
            ?? throw new JsonException("Failed to parse user info response.");

        session.Username = response.Username;
        session.UserId = response.Id;
        session.LobbyId = response.LobbyId;

        return (session.UserId, session.Username, session.LobbyId);
    }
}