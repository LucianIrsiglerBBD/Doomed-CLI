using DoomedCLI.Utility.HTTP;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DoomedCLI.Utility;

public static class CurrentUser
{
    private sealed record MeResponse([property: JsonPropertyName("username")] string Username);

    public static void SetUsername(string username)
    {
        UserSession.Instance.Username = username;
    }


    public static async Task<string> GetUsernameAsync(CancellationToken cancellationToken = default, bool refresh = false)
    {
        if (!refresh && !string.IsNullOrWhiteSpace(UserSession.Instance.Username))
        {
            return UserSession.Instance.Username;
        }

        var host = Environment.GetEnvironmentVariable("HOST_IP") ?? string.Empty;
        var httpHandler = new HttpHandler(new HttpClient
        {
            BaseAddress = new Uri(host)
        });

        var result = await httpHandler.GetAsync("api/users/me", cancellationToken).ConfigureAwait(false);
        var response = JsonSerializer.Deserialize<MeResponse>(result)
            ?? throw new JsonException("Failed to parse username response.");

        SetUsername(response.Username);
        return response.Username;
    }
}