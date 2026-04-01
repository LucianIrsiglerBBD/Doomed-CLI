using DoomedCLI;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Threading;

internal class TestGameCommand : Command<TestGameCommand.Settings>
{
    public sealed class Settings : CommandSettings { }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        AnsiConsole.MarkupLine("[yellow]Starting test game (no auth, no backend)...[/]");
        AnsiConsole.MarkupLine("[grey]Two bots (Bot_Alpha, Bot_Beta) will wander the map.[/]");
        GameRunner.RunTest();
        return 0;
    }
}
