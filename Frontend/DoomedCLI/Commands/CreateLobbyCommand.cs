using Spectre.Console.Cli;
using DoomedCLI.Utility.HTTP;
using Spectre.Console;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DoomedCLI.Utility;

internal class CreateLobbyCommand : AsyncCommand<CreateLobbyCommand.Settings>
{

    internal record LobbyCreateRequest(
        [property: JsonPropertyName("username")] string Name,
        [property: JsonPropertyName("mapId")] int MapId,
        [property: JsonPropertyName("durationInMinutes")] int DurationInMinutes);

    private readonly HttpHandler _httpClientHandler = new(new HttpClient
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("HOST_IP") ?? "")
    }
    );

    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[mapId]")]
        [DefaultValue(1)]
        [Description("Map id to use for the lobby")]
        public int MapId { get; init; } = 1;

        [CommandOption("--duration|-d <MINUTES>")]
        [DefaultValue(30)]
        [Description("Lobby duration in minutes")]
        public int DurationInMinutes { get; init; } = 30;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        try
        {
            var username = await CurrentUser.GetUsernameAsync(cancellation);

            var request = new LobbyCreateRequest(username, settings.MapId, settings.DurationInMinutes);

            var requestJson = JsonSerializer.Serialize(request);

            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClientHandler.PostAsync("api/lobbies", content, cancellation);

            AnsiConsole.MarkupLine($"[green]{response}[/]");

            return 0;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
            return 1;
        }
    }
}