// <copyright file="StrategyStatusCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Strategy;

/// <summary>
/// Command to show the strategy engine status.
/// </summary>
public sealed class StrategyStatusCommand : AsyncCommand
{
    private readonly IStrategyEngine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyStatusCommand"/> class.
    /// </summary>
    /// <param name="engine">Strategy engine.</param>
    public StrategyStatusCommand(IStrategyEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        var strategies = await _engine.GetStrategiesAsync(cancellationToken);
        var enabledStrategies = strategies.Where(s => s.IsEnabled).ToList();

        var statusMarkup = _engine.IsRunning
            ? "[green]● Running[/]"
            : "[red]○ Stopped[/]";

        var panel = new Panel(new Markup(
            $"[bold]Engine Status:[/] {statusMarkup}\n" +
            $"[bold]Total Strategies:[/] {strategies.Count}\n" +
            $"[bold]Enabled Strategies:[/] {enabledStrategies.Count}\n" +
            $"[bold]Disabled Strategies:[/] {strategies.Count - enabledStrategies.Count}"))
        {
            Header = new PanelHeader("[bold yellow]STRATEGY ENGINE STATUS[/]"),
            Border = BoxBorder.Rounded,
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        if (enabledStrategies.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Enabled Strategies:[/]");
            foreach (var strategy in enabledStrategies.OrderBy(s => s.Name))
            {
                var symbols = string.Join(", ", strategy.Symbols);
                AnsiConsole.MarkupLine($"  [green]●[/] {strategy.Name} ({strategy.Type}) - {symbols} @ {strategy.Timeframe}");
            }

            AnsiConsole.WriteLine();
        }

        if (_engine.IsRunning)
        {
            AnsiConsole.MarkupLine("[dim]Use 'strategy stop' to stop the engine.[/]");
        }
        else if (enabledStrategies.Count > 0)
        {
            AnsiConsole.MarkupLine("[dim]Use 'strategy start' to start executing strategies.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]Enable strategies using 'strategy enable <name>' before starting.[/]");
        }

        return 0;
    }
}
