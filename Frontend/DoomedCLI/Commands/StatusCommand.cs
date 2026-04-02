using Spectre.Console.Cli;
using Spectre.Console;
using DoomedCLI.Utility.HTTP;
using System;

internal class StatusCommand : AsyncCommand<StatusCommand.Settings>
{

    internal record LoginRequest(string Name);

    private readonly HttpHandler _httpClientHandler = new(new HttpClient
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("HOST_IP")??"")
    }
    );


    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        try
        {
            var result = await _httpClientHandler.GetAsync("checkGoogleAuth", cancellation);
            AnsiConsole.MarkupLine($"Server response:{result}");
            return 0;
        }
        catch (Exception)
        {
            throw;
        }
    }
}