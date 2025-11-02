// <copyright file="PortfolioHistoryCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Portfolio;

/// <summary>
/// Command to display trade history.
/// </summary>
public sealed class PortfolioHistoryCommand : AsyncCommand<PortfolioHistoryCommand.Settings>
{
    private readonly IPortfolioManager _portfolioManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PortfolioHistoryCommand"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public PortfolioHistoryCommand(IPortfolioManager portfolioManager)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        DateTime? startDate = null;
        DateTime? endDate = null;

        if (!string.IsNullOrEmpty(settings.StartDate))
        {
            if (DateTime.TryParse(settings.StartDate, out var start))
            {
                startDate = start;
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Invalid start date format. Use YYYY-MM-DD.");
                return 1;
            }
        }

        if (!string.IsNullOrEmpty(settings.EndDate))
        {
            if (DateTime.TryParse(settings.EndDate, out var end))
            {
                endDate = end;
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Invalid end date format. Use YYYY-MM-DD.");
                return 1;
            }
        }

        var trades = await _portfolioManager.GetTradeHistoryAsync(
            startDate,
            endDate,
            settings.Symbol,
            settings.Strategy);

        if (trades.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No trades found matching the criteria.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Date[/]")
            .AddColumn("[bold]Symbol[/]")
            .AddColumn("[bold]Side[/]", c => c.Centered())
            .AddColumn("[bold]Qty[/]", c => c.RightAligned())
            .AddColumn("[bold]Entry[/]", c => c.RightAligned())
            .AddColumn("[bold]Exit[/]", c => c.RightAligned())
            .AddColumn("[bold]P&L[/]", c => c.RightAligned())
            .AddColumn("[bold]Strategy[/]");

        var totalPnL = 0m;

        foreach (var trade in trades.Take(settings.Limit))
        {
            var sideMarkup = trade.Side.ToString() == "Buy"
                ? "[green]BUY[/]"
                : "[red]SELL[/]";

            var pnl = trade.RealizedPnL;
            totalPnL += pnl;

            var pnlColor = pnl >= 0 ? "green" : "red";
            var pnlSign = pnl >= 0 ? "+" : string.Empty;

            table.AddRow(
                trade.ExitTime.ToString("yyyy-MM-dd HH:mm"),
                trade.Symbol,
                sideMarkup,
                trade.Quantity.ToString("F2"),
                $"${trade.EntryPrice:F2}",
                $"${trade.ExitPrice:F2}",
                $"[{pnlColor}]{pnlSign}${pnl:N2}[/]",
                trade.StrategyName);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Summary
        var winningTrades = trades.Count(t => t.RealizedPnL > 0);
        var losingTrades = trades.Count(t => t.RealizedPnL < 0);
        var winRate = trades.Count > 0 ? ((decimal)winningTrades / trades.Count) * 100m : 0m;

        var totalPnLColor = totalPnL >= 0 ? "green" : "red";
        var totalPnLSign = totalPnL >= 0 ? "+" : string.Empty;

        AnsiConsole.MarkupLine($"[dim]Total Trades: {trades.Count} | " +
            $"Wins: {winningTrades} | Losses: {losingTrades} | " +
            $"Win Rate: {winRate:F1}% | " +
            $"Total P&L: [{totalPnLColor}]{totalPnLSign}${totalPnL:N2}[/{totalPnLColor}][/]");

        if (trades.Count > settings.Limit)
        {
            AnsiConsole.MarkupLine($"[dim]Showing {settings.Limit} of {trades.Count} trades. Use --limit to show more.[/]");
        }

        return 0;
    }

    /// <summary>
    /// Settings for the portfolio history command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the start date filter.
        /// </summary>
        [CommandOption("--start-date")]
        [Description("Start date filter (YYYY-MM-DD)")]
        public string? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date filter.
        /// </summary>
        [CommandOption("--end-date")]
        [Description("End date filter (YYYY-MM-DD)")]
        public string? EndDate { get; set; }

        /// <summary>
        /// Gets or sets the symbol filter.
        /// </summary>
        [CommandOption("--symbol|-s")]
        [Description("Symbol filter (e.g., SPY)")]
        public string? Symbol { get; set; }

        /// <summary>
        /// Gets or sets the strategy filter.
        /// </summary>
        [CommandOption("--strategy")]
        [Description("Strategy name filter")]
        public string? Strategy { get; set; }

        /// <summary>
        /// Gets or sets the result limit.
        /// </summary>
        [CommandOption("--limit|-l")]
        [Description("Maximum number of trades to display")]
        [DefaultValue(20)]
        public int Limit { get; set; } = 20;
    }
}
