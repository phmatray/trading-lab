// <copyright file="StrategyListCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Strategy;

/// <summary>
/// Command to list all registered strategies.
/// </summary>
public sealed class StrategyListCommand : AsyncCommand
{
    private readonly IStrategyEngine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyListCommand"/> class.
    /// </summary>
    /// <param name="engine">Strategy engine.</param>
    public StrategyListCommand(IStrategyEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var strategies = await _engine.GetStrategiesAsync();

        if (strategies.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No strategies registered.[/]");
            AnsiConsole.MarkupLine("[dim]Strategies will be available after they are configured and loaded.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Status[/]", c => c.Centered())
            .AddColumn("[bold]Symbols[/]")
            .AddColumn("[bold]Timeframe[/]", c => c.Centered());

        foreach (var strategy in strategies.OrderBy(s => s.Name))
        {
            var statusMarkup = strategy.IsEnabled
                ? "[green]● Active[/]"
                : "[red]○ Disabled[/]";

            var symbols = string.Join(", ", strategy.Symbols);

            table.AddRow(
                strategy.Name,
                strategy.Type,
                statusMarkup,
                symbols,
                strategy.Timeframe);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Summary
        var activeCount = strategies.Count(s => s.IsEnabled);
        var totalCount = strategies.Count;

        AnsiConsole.MarkupLine($"[dim]Total: {totalCount} strategies ({activeCount} active, {totalCount - activeCount} disabled)[/]");

        return 0;
    }
}
