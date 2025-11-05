// <copyright file="BacktestReportCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Backtest;

namespace TradingBot.Cli.Commands.Backtest;

/// <summary>
/// Command to display backtest report.
/// </summary>
public sealed class BacktestReportCommand : AsyncCommand<BacktestReportCommand.Settings>
{
    private readonly IBacktestingEngine _backtestingEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="BacktestReportCommand"/> class.
    /// </summary>
    /// <param name="backtestingEngine">Backtesting engine instance.</param>
    public BacktestReportCommand(IBacktestingEngine backtestingEngine)
    {
        _backtestingEngine = backtestingEngine ?? throw new ArgumentNullException(nameof(backtestingEngine));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var backtestId = settings.BacktestId ?? "latest";

        BacktestResult? result;
        if (backtestId.Equals("latest", StringComparison.OrdinalIgnoreCase))
        {
            result = await _backtestingEngine.GetLatestBacktestResultAsync();
        }
        else
        {
            result = await _backtestingEngine.GetBacktestResultAsync(backtestId);
        }

        if (result == null)
        {
            AnsiConsole.MarkupLine("[red]Error: No backtest results found[/]");
            if (!backtestId.Equals("latest", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine($"[red]Backtest ID '{backtestId}' not found[/]");
            }

            return 1;
        }

        AnsiConsole.Write(
            new FigletText("Backtest Report")
                .Color(Color.Blue));

        DisplayBacktestReport(result);

        return 0;
    }

    private static void DisplayBacktestReport(BacktestResult result)
    {
        // Header Info
        var headerPanel = new Panel(
            new Markup($"[cyan]Backtest ID:[/] {result.BacktestId}\n" +
                       $"[cyan]Strategy:[/] {result.StrategyName}\n" +
                       $"[cyan]Symbol:[/] {result.Symbol}\n" +
                       $"[cyan]Period:[/] {result.StartDate:yyyy-MM-dd} to {result.EndDate:yyyy-MM-dd}\n" +
                       $"[cyan]Duration:[/] {result.Duration.TotalSeconds:F2}s"))
        {
            Header = new PanelHeader("[yellow]Backtest Information[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
        };

        AnsiConsole.Write(headerPanel);
        AnsiConsole.WriteLine();

        // Performance Summary
        var perfTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green)
            .Title("[yellow]Performance Summary[/]");

        perfTable.AddColumn("[bold]Metric[/]");
        perfTable.AddColumn("[bold]Value[/]");

        perfTable.AddRow("Initial Capital", $"[cyan]${result.InitialCapital:N2}[/]");
        perfTable.AddRow("Final Equity", $"[cyan]${result.FinalEquity:N2}[/]");
        perfTable.AddRow("Total P&L", FormatPnL(result.TotalPnL));
        perfTable.AddRow("Total Return", FormatReturn(result.TotalReturn));

        AnsiConsole.Write(perfTable);
        AnsiConsole.WriteLine();

        // Trading Statistics
        var statsTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .Title("[yellow]Trading Statistics[/]");

        statsTable.AddColumn("[bold]Statistic[/]");
        statsTable.AddColumn("[bold]Value[/]");

        var perf = result.Performance;
        statsTable.AddRow("Total Trades", $"[cyan]{perf.TotalTrades}[/]");
        statsTable.AddRow("Winning Trades", $"[green]{perf.WinningTrades}[/]");
        statsTable.AddRow("Losing Trades", $"[red]{perf.LosingTrades}[/]");
        statsTable.AddRow("Win Rate", $"[cyan]{perf.WinRate:F2}%[/]");
        statsTable.AddRow("Profit Factor", $"[cyan]{perf.ProfitFactor:F2}[/]");
        statsTable.AddRow("Average Win", $"[green]${perf.AverageWin:N2}[/]");
        statsTable.AddRow("Average Loss", $"[red]${perf.AverageLoss:N2}[/]");

        AnsiConsole.Write(statsTable);
        AnsiConsole.WriteLine();

        // Top Trades
        if (result.Trades.Count > 0)
        {
            var topWins = result.Trades
                .Where(t => t.RealizedPnL > 0)
                .OrderByDescending(t => t.RealizedPnL)
                .Take(5)
                .ToList();

            var topLosses = result.Trades
                .Where(t => t.RealizedPnL < 0)
                .OrderBy(t => t.RealizedPnL)
                .Take(5)
                .ToList();

            if (topWins.Count > 0)
            {
                var winsTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Green)
                    .Title("[yellow]Top 5 Winning Trades[/]");

                winsTable.AddColumn("[bold]Date[/]");
                winsTable.AddColumn("[bold]Symbol[/]");
                winsTable.AddColumn("[bold]Quantity[/]");
                winsTable.AddColumn("[bold]Entry[/]");
                winsTable.AddColumn("[bold]Exit[/]");
                winsTable.AddColumn("[bold]P&L[/]");

                foreach (var trade in topWins)
                {
                    winsTable.AddRow(
                        trade.ExitTime.ToString("yyyy-MM-dd"),
                        trade.Symbol,
                        trade.Quantity.ToString("N2"),
                        $"${trade.EntryPrice:N2}",
                        $"${trade.ExitPrice:N2}",
                        $"[green]+${trade.RealizedPnL:N2}[/]");
                }

                AnsiConsole.Write(winsTable);
                AnsiConsole.WriteLine();
            }

            if (topLosses.Count > 0)
            {
                var lossesTable = new Table()
                    .Border(TableBorder.Rounded)
                    .BorderColor(Color.Red)
                    .Title("[yellow]Top 5 Losing Trades[/]");

                lossesTable.AddColumn("[bold]Date[/]");
                lossesTable.AddColumn("[bold]Symbol[/]");
                lossesTable.AddColumn("[bold]Quantity[/]");
                lossesTable.AddColumn("[bold]Entry[/]");
                lossesTable.AddColumn("[bold]Exit[/]");
                lossesTable.AddColumn("[bold]P&L[/]");

                foreach (var trade in topLosses)
                {
                    lossesTable.AddRow(
                        trade.ExitTime.ToString("yyyy-MM-dd"),
                        trade.Symbol,
                        trade.Quantity.ToString("N2"),
                        $"${trade.EntryPrice:N2}",
                        $"${trade.ExitPrice:N2}",
                        $"[red]${trade.RealizedPnL:N2}[/]");
                }

                AnsiConsole.Write(lossesTable);
                AnsiConsole.WriteLine();
            }
        }

        // Equity Curve Summary
        if (result.EquityCurve.Count > 0)
        {
            var curvePanel = new Panel(
                new Markup($"[cyan]Start Equity:[/] ${result.EquityCurve.First().Equity:N2}\n" +
                           $"[cyan]End Equity:[/] ${result.EquityCurve.Last().Equity:N2}\n" +
                           $"[cyan]Peak Equity:[/] ${result.EquityCurve.Max(e => e.Equity):N2}\n" +
                           $"[cyan]Data Points:[/] {result.EquityCurve.Count}"))
            {
                Header = new PanelHeader("[yellow]Equity Curve Summary[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = new Style(Color.Blue),
            };

            AnsiConsole.Write(curvePanel);
        }
    }

    private static string FormatPnL(decimal pnl)
    {
        var color = pnl >= 0 ? "green" : "red";
        var sign = pnl >= 0 ? "+" : string.Empty;
        return $"[{color}]{sign}${pnl:N2}[/]";
    }

    private static string FormatReturn(decimal returnPct)
    {
        var color = returnPct >= 0 ? "green" : "red";
        var sign = returnPct >= 0 ? "+" : string.Empty;
        return $"[{color}]{sign}{returnPct:F2}%[/]";
    }

    /// <summary>
    /// Settings for the backtest report command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the backtest ID.
        /// </summary>
        [CommandArgument(0, "[backtest-id]")]
        [Description("Backtest ID or 'latest' for most recent (default: latest)")]
        public string? BacktestId { get; set; }
    }
}
