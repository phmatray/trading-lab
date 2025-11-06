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
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
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
        // Create main layout
        var mainLayout = new Layout("Root")
            .SplitRows(
                new Layout("Header").MinimumSize(8),
                new Layout("Body").MinimumSize(15),
                new Layout("Trades"));

        // Header Info with aligned grid
        var headerGrid = new Grid()
            .AddColumn(new GridColumn().Width(15).LeftAligned())
            .AddColumn(new GridColumn().NoWrap().RightAligned());

        headerGrid.AddRow("[cyan]Backtest ID:[/]", $"{result.BacktestId}");
        headerGrid.AddRow("[cyan]Strategy:[/]", $"{result.StrategyName}");
        headerGrid.AddRow("[cyan]Symbol:[/]", $"{result.Symbol}");
        headerGrid.AddRow("[cyan]Period:[/]", $"{result.StartDate:yyyy-MM-dd} to {result.EndDate:yyyy-MM-dd}");
        headerGrid.AddRow("[cyan]Duration:[/]", $"{result.Duration.TotalSeconds:F2}s");

        var headerPanel = new Panel(headerGrid)
        {
            Header = new PanelHeader("[yellow]Backtest Information[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
        };
        headerPanel.Expand();
        mainLayout["Header"].Update(headerPanel);

        // Body with side-by-side layout
        var bodyLayout = mainLayout["Body"]
            .SplitColumns(
                new Layout("Performance").Ratio(1),
                new Layout("Statistics").Ratio(1));

        // Performance Summary with aligned grid
        var perfGrid = new Grid()
            .AddColumn(new GridColumn().Width(18).LeftAligned())
            .AddColumn(new GridColumn().NoWrap().RightAligned());

        perfGrid.AddRow("[bold]Initial Capital:[/]", $"[cyan]${result.InitialCapital:N2}[/]");
        perfGrid.AddRow("[bold]Final Equity:[/]", $"[cyan]${result.FinalEquity:N2}[/]");
        perfGrid.AddRow("[bold]Total P&L:[/]", FormatPnL(result.TotalPnL));
        perfGrid.AddRow("[bold]Total Return:[/]", FormatReturn(result.TotalReturn));

        var perfPanel = new Panel(perfGrid)
        {
            Header = new PanelHeader("[green]Performance Summary[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Green),
        };
        perfPanel.Expand();
        bodyLayout["Performance"].Update(perfPanel);

        // Trading Statistics with aligned grid
        var perf = result.Performance;
        var statsGrid = new Grid()
            .AddColumn(new GridColumn().Width(18).LeftAligned())
            .AddColumn(new GridColumn().NoWrap().RightAligned());

        statsGrid.AddRow("[bold]Total Trades:[/]", $"[cyan]{perf.TotalTrades}[/]");
        statsGrid.AddRow("[bold]Winning Trades:[/]", $"[green]{perf.WinningTrades}[/]");
        statsGrid.AddRow("[bold]Losing Trades:[/]", $"[red]{perf.LosingTrades}[/]");
        statsGrid.AddRow("[bold]Win Rate:[/]", $"[cyan]{perf.WinRate:F2}%[/]");
        statsGrid.AddRow("[bold]Profit Factor:[/]", $"[cyan]{perf.ProfitFactor:F2}[/]");
        statsGrid.AddRow("[bold]Average Win:[/]", $"[green]${perf.AverageWin:N2}[/]");
        statsGrid.AddRow("[bold]Average Loss:[/]", $"[red]${perf.AverageLoss:N2}[/]");

        var statsPanel = new Panel(statsGrid)
        {
            Header = new PanelHeader("[cyan]Trading Statistics[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
        };
        statsPanel.Expand();
        bodyLayout["Statistics"].Update(statsPanel);

        // Top Trades - Side by side layout
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

            if (topWins.Count > 0 || topLosses.Count > 0)
            {
                var tradesLayout = mainLayout["Trades"]
                    .SplitColumns(
                        new Layout("WinningTrades").Ratio(1),
                        new Layout("LosingTrades").Ratio(1));

                // Winning trades table
                if (topWins.Count > 0)
                {
                    var winsTable = new Table()
                        .Border(TableBorder.Rounded)
                        .BorderColor(Color.Green)
                        .AddColumn("[bold]Date[/]")
                        .AddColumn("[bold]Symbol[/]")
                        .AddColumn("[bold]Qty[/]", c => c.RightAligned())
                        .AddColumn("[bold]Entry[/]", c => c.RightAligned())
                        .AddColumn("[bold]Exit[/]", c => c.RightAligned())
                        .AddColumn("[bold]P&L[/]", c => c.RightAligned());

                    foreach (var trade in topWins)
                    {
                        winsTable.AddRow(
                            trade.ExitTime.ToString("MM/dd"),
                            trade.Symbol,
                            trade.Quantity.ToString("N0"),
                            $"${trade.EntryPrice:N2}",
                            $"${trade.ExitPrice:N2}",
                            $"[green]+${trade.RealizedPnL:N2}[/]");
                    }

                    var winsPanel = new Panel(winsTable)
                    {
                        Header = new PanelHeader("[green]Top 5 Winning Trades[/]"),
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Green),
                    };
                    winsPanel.Expand();
                    tradesLayout["WinningTrades"].Update(winsPanel);
                }
                else
                {
                    var noWinsPanel = new Panel(
                        Align.Center(
                            new Markup("[dim]No winning trades[/]"),
                            VerticalAlignment.Middle))
                    {
                        Header = new PanelHeader("[green]Top 5 Winning Trades[/]"),
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Green),
                    };
                    noWinsPanel.Expand();
                    tradesLayout["WinningTrades"].Update(noWinsPanel);
                }

                // Losing trades table
                if (topLosses.Count > 0)
                {
                    var lossesTable = new Table()
                        .Border(TableBorder.Rounded)
                        .BorderColor(Color.Red)
                        .AddColumn("[bold]Date[/]")
                        .AddColumn("[bold]Symbol[/]")
                        .AddColumn("[bold]Qty[/]", c => c.RightAligned())
                        .AddColumn("[bold]Entry[/]", c => c.RightAligned())
                        .AddColumn("[bold]Exit[/]", c => c.RightAligned())
                        .AddColumn("[bold]P&L[/]", c => c.RightAligned());

                    foreach (var trade in topLosses)
                    {
                        lossesTable.AddRow(
                            trade.ExitTime.ToString("MM/dd"),
                            trade.Symbol,
                            trade.Quantity.ToString("N0"),
                            $"${trade.EntryPrice:N2}",
                            $"${trade.ExitPrice:N2}",
                            $"[red]${trade.RealizedPnL:N2}[/]");
                    }

                    var lossesPanel = new Panel(lossesTable)
                    {
                        Header = new PanelHeader("[red]Top 5 Losing Trades[/]"),
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Red),
                    };
                    lossesPanel.Expand();
                    tradesLayout["LosingTrades"].Update(lossesPanel);
                }
                else
                {
                    var noLossesPanel = new Panel(
                        Align.Center(
                            new Markup("[dim]No losing trades[/]"),
                            VerticalAlignment.Middle))
                    {
                        Header = new PanelHeader("[red]Top 5 Losing Trades[/]"),
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Red),
                    };
                    noLossesPanel.Expand();
                    tradesLayout["LosingTrades"].Update(noLossesPanel);
                }
            }
        }

        // Render the main layout
        AnsiConsole.Write(mainLayout);
        AnsiConsole.WriteLine();

        // Equity Curve Summary (below the layout)
        if (result.EquityCurve.Count > 0)
        {
            var curveGrid = new Grid()
                .AddColumn(new GridColumn().Width(15).LeftAligned())
                .AddColumn(new GridColumn().NoWrap().RightAligned());

            curveGrid.AddRow("[cyan]Start Equity:[/]", $"${result.EquityCurve.First().Equity:N2}");
            curveGrid.AddRow("[cyan]End Equity:[/]", $"${result.EquityCurve.Last().Equity:N2}");
            curveGrid.AddRow("[cyan]Peak Equity:[/]", $"${result.EquityCurve.Max(e => e.Equity):N2}");
            curveGrid.AddRow("[cyan]Data Points:[/]", $"{result.EquityCurve.Count}");

            var curvePanel = new Panel(curveGrid)
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
