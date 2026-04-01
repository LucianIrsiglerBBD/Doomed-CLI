using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DoomedCLI.Utility;
using DoomedCLI.Utility.HTTP;

internal class JoinLobbyCommand : AsyncCommand<JoinLobbyCommand.Settings>
{
    private sealed record JoinRequest([property: JsonPropertyName("userId")] int UserId);

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "<lobbyCode>")]
        [Description("6-character lobby code to join")]
        public string LobbyCode { get; init; } = string.Empty;
    }

    private readonly HttpHandler _http = new(new HttpClient
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("HOST_IP") ?? "")
    });

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        try
        {
            var (userId, username, _) = await CurrentUser.GetUserInfoAsync(cancellation, refresh: true);
            if (userId == -1)
            {
                AnsiConsole.MarkupLine("[red]Not logged in. Run 'login' first.[/]");
                return 1;
            }

            var body = JsonSerializer.Serialize(new JoinRequest(userId));
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync($"api/lobbies/{settings.LobbyCode}/join", content, cancellation);

            AnsiConsole.MarkupLine($"[green]Joined lobby {settings.LobbyCode}. {response}[/]");

            // Refresh session so LobbyId is up to date
            await CurrentUser.GetUserInfoAsync(cancellation, refresh: true);
            return 0;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            return 1;
        }
    }
}
