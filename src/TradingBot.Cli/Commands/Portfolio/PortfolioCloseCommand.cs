// <copyright file="PortfolioCloseCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Portfolio;

/// <summary>
/// Command to close positions.
/// </summary>
public sealed class PortfolioCloseCommand : AsyncCommand<PortfolioCloseCommand.Settings>
{
    private readonly IPortfolioManager _portfolioManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PortfolioCloseCommand"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public PortfolioCloseCommand(IPortfolioManager portfolioManager)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.All)
        {
            return await CloseAllPositionsAsync();
        }

        if (string.IsNullOrEmpty(settings.Symbol))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Either specify --symbol or use --all to close all positions.");
            return 1;
        }

        return await ClosePositionAsync(settings.Symbol);
    }

    private async Task<int> ClosePositionAsync(string symbol)
    {
        // Confirm before closing
        if (!AnsiConsole.Confirm($"Close position for [cyan]{symbol}[/]?", defaultValue: false))
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
            return 0;
        }

        var closed = await _portfolioManager.ClosePositionAsync(symbol);

        if (closed)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Position for [cyan]{symbol}[/] closed successfully.");
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]Error:[/] Position not found for symbol: [cyan]{symbol}[/]");
        AnsiConsole.MarkupLine("[dim]Use 'portfolio show' to see open positions.[/]");
        return 1;
    }

    private async Task<int> CloseAllPositionsAsync()
    {
        var positions = await _portfolioManager.GetPositionsAsync();

        if (positions.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No open positions to close.[/]");
            return 0;
        }

        // Show positions that will be closed
        AnsiConsole.MarkupLine($"[yellow]This will close {positions.Count} position(s):[/]");
        foreach (var position in positions)
        {
            AnsiConsole.MarkupLine($"  • {position.Symbol} ({position.Quantity} shares)");
        }

        AnsiConsole.WriteLine();

        // Confirm before closing all
        if (!AnsiConsole.Confirm("Are you sure you want to close [bold red]ALL[/] positions?", defaultValue: false))
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
            return 0;
        }

        var count = await AnsiConsole.Status()
            .StartAsync("Closing positions...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("yellow"));

                return await _portfolioManager.CloseAllPositionsAsync();
            });

        AnsiConsole.MarkupLine($"[green]✓[/] Successfully closed {count} position(s).");
        return 0;
    }

    /// <summary>
    /// Settings for the portfolio close command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the symbol to close.
        /// </summary>
        [CommandOption("--symbol|-s")]
        [Description("Symbol of the position to close")]
        public string? Symbol { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to close all positions.
        /// </summary>
        [CommandOption("--all")]
        [Description("Close all open positions")]
        public bool All { get; set; }
    }
}
