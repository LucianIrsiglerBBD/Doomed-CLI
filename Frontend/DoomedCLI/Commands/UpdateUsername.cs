using Spectre.Console.Cli;
using DoomedCLI.Utility.HTTP;
using Spectre.Console;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DoomedCLI.Utility;

internal class UpdateUsernameCommand : AsyncCommand<UpdateUsernameCommand.Settings>
{

    internal record UsernameRequest(
        [property: JsonPropertyName("username")] string Name,
        [property: JsonPropertyName("email")] string Email);

    private readonly HttpHandler _httpClientHandler = new(new HttpClient
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("HOST_IP") ?? "")
    }
    );


    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[username]")]
        [Required]
        [Description("Username of the user to update")]
        public string UserName { get; init; } = string.Empty;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        try
        {
            var email = File.ReadAllText(".email");

            if (string.IsNullOrEmpty(email))
            {
                AnsiConsole.MarkupLine("[red]You must be logged in to create a username.[/]");
                return 1;
            }

            if (string.IsNullOrEmpty(settings.UserName))
            {
                AnsiConsole.MarkupLine("[red]Username cannot be empty.[/]");
                return 1;
            }

            var request = new UsernameRequest(settings.UserName, email);

            var requestJson = JsonSerializer.Serialize(request);

            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            var response = await _httpClientHandler.PatchAsync("api/users/", content, cancellation);

            AnsiConsole.MarkupLine($"[green]{response}[/]");
            
            CurrentUser.SetUsername(settings.UserName);

            return 0;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]An error occurred: {ex.Message}[/]");
            return 1;
        }
    }
}