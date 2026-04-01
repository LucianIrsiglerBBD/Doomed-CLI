using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<HelpCommand>("help");
    config.AddCommand<LoginCommand>("login");
    config.AddCommand<StatusCommand>("status");
    config.AddCommand<UpdateUsernameCommand>("username");
    config.AddCommand<CreateLobbyCommand>("createlobby");
    config.AddCommand<JoinLobbyCommand>("joinlobby");
    config.AddCommand<StartGameCommand>("startgame");
    config.AddCommand<TestGameCommand>("testgame");
});

while (true)
{
    var input = AnsiConsole.Prompt(
        new TextPrompt<string>("[green]doomed>[/]")
            .AllowEmpty());

    if (string.IsNullOrWhiteSpace(input))
        continue;

    if (input is "exit" or "quit")
        break;

    // Don't add to history until successful
    var commandArgs = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

    try
    {
        var result = await app.RunAsync(commandArgs);
    }
    catch (Exception ex)
    {
        AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
    }
}
 