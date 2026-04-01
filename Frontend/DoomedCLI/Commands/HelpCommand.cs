using Spectre.Console.Cli;
using Spectre.Console;
internal class HelpCommand : Command<HelpCommand.Settings>
{
    protected const string _helpCommand =
    """
    The following commands exists:
        [blue]help[/]                        Displays this command
    """;

    public class Settings : CommandSettings { }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        AnsiConsole.MarkupLine(_helpCommand);
        return 0;
    }
}