using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DoomedCLI;
using DoomedCLI.Utility;
using DoomedCLI.Utility.HTTP;

internal class StartGameCommand : AsyncCommand<StartGameCommand.Settings>
{
    private sealed record StartLobbyRequest([property: JsonPropertyName("hostUserId")] int HostUserId);

    private sealed record LobbyResponse(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("started")] bool Started,
        [property: JsonPropertyName("hostUser")] HostUserInfo HostUser,
        [property: JsonPropertyName("map")] MapInfo Map);

    private sealed record HostUserInfo([property: JsonPropertyName("id")] int Id);

    private sealed record MapInfo(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("data")] string Data);

    public class Settings : CommandSettings { }

    private readonly HttpHandler _http = new(new HttpClient
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("HOST_IP") ?? "")
    });

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        try
        {
            var (userId, username, lobbyId) = await CurrentUser.GetUserInfoAsync(cancellation, refresh: true);

            if (userId == -1)
            {
                AnsiConsole.MarkupLine("[red]Not logged in. Run 'login' first.[/]");
                return 1;
            }
            if (lobbyId == -1)
            {
                AnsiConsole.MarkupLine("[red]Not in a lobby. Use 'createlobby' or 'joinlobby <code>' first.[/]");
                return 1;
            }

            var lobbyJson = await _http.GetAsync($"api/lobbies/{lobbyId}", cancellation);
            var lobby = JsonSerializer.Deserialize<LobbyResponse>(lobbyJson)
                ?? throw new InvalidOperationException("Could not parse lobby response.");

            bool isHost = lobby.HostUser.Id == userId;

            if (isHost && !lobby.Started)
            {
                var startBody = JsonSerializer.Serialize(new StartLobbyRequest(userId));
                var content = new StringContent(startBody, Encoding.UTF8, "application/json");
                await _http.PostAsync($"api/lobbies/{lobbyId}/start", content, cancellation);
                AnsiConsole.MarkupLine("[green]Game started![/]");
            }
            else if (!lobby.Started)
            {
                AnsiConsole.MarkupLine("[yellow]Waiting for host to start the game...[/]");
                // Poll until the lobby is started
                while (!lobby.Started)
                {
                    await Task.Delay(2000, cancellation);
                    lobbyJson = await _http.GetAsync($"api/lobbies/{lobbyId}", cancellation);
                    lobby = JsonSerializer.Deserialize<LobbyResponse>(lobbyJson)
                        ?? throw new InvalidOperationException("Could not parse lobby response.");
                }
                AnsiConsole.MarkupLine("[green]Game started — entering...[/]");
            }

            var mapData = lobby.Map.Data;
            var map = string.IsNullOrWhiteSpace(mapData)
                ? new GameMap("test.txt")
                : GameMap.FromString(mapData);

            var hostIp = Environment.GetEnvironmentVariable("HOST_IP") ?? string.Empty;
            GameRunner.Run(map, username, lobbyId.ToString(), hostIp);
            return 0;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]{ex.Message}[/]");
            return 1;
        }
    }
}
