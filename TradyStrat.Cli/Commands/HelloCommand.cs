using Spectre.Console;
using Spectre.Console.Cli;

namespace TradyStrat.Cli.Commands;

internal sealed class HelloCommand : Command
{
    public override int Execute(CommandContext context)
    {
        AnsiConsole.MarkupLine("[green]TradyStrat.Cli is wired.[/]");
        return 0;
    }
}
