// <copyright file="PerformanceShowCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Performance;

/// <summary>
/// Command to display performance metrics.
/// </summary>
public sealed class PerformanceShowCommand : AsyncCommand
{
    private readonly IPortfolioManager _portfolioManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceShowCommand"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public PerformanceShowCommand(IPortfolioManager portfolioManager)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var metrics = await AnsiConsole.Status()
            .StartAsync("Calculating performance metrics...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));

                return await _portfolioManager.GetPerformanceMetricsAsync();
            });

        // Performance Summary Panel
        var summaryPanel = new Panel(new Markup(
            $"[bold]Total Return:[/] {FormatReturn(metrics.TotalReturn)}\n" +
            $"[bold]Sharpe Ratio:[/] {metrics.SharpeRatio:F2}\n" +
            $"[bold]Sortino Ratio:[/] {metrics.SortinoRatio:F2}\n" +
            $"[bold]Calmar Ratio:[/] {metrics.CalmarRatio:F2}\n" +
            $"[bold]Max Drawdown:[/] {FormatPercent(metrics.MaxDrawdown)}\n" +
            $"[bold]Profit Factor:[/] {metrics.ProfitFactor:F2}"))
        {
            Header = new PanelHeader("[bold yellow]PERFORMANCE SUMMARY[/]"),
            Border = BoxBorder.Rounded,
        };

        AnsiConsole.Write(summaryPanel);
        AnsiConsole.WriteLine();

        // Trading Statistics Table
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Metric[/]")
            .AddColumn("[bold]Value[/]", c => c.RightAligned())
            .AddColumn("[bold]Details[/]");

        table.AddRow(
            "Total Trades",
            metrics.TotalTrades.ToString(),
            $"{metrics.WinningTrades} wins, {metrics.LosingTrades} losses");

        var winRateColor = metrics.WinRate >= 50 ? "green" : "yellow";
        table.AddRow(
            "Win Rate",
            $"[{winRateColor}]{metrics.WinRate:F1}%[/]",
            metrics.WinRate >= 60 ? "Excellent" : metrics.WinRate >= 50 ? "Good" : "Needs improvement");

        table.AddRow(
            "Average Win",
            $"[green]+${metrics.AverageWin:N2}[/]",
            "Per winning trade");

        table.AddRow(
            "Average Loss",
            $"[red]${metrics.AverageLoss:N2}[/]",
            "Per losing trade");

        var expectancy = (metrics.WinRate / 100m * metrics.AverageWin) +
                        ((100m - metrics.WinRate) / 100m * metrics.AverageLoss);
        var expectancyColor = expectancy > 0 ? "green" : "red";
        table.AddRow(
            "Expectancy",
            $"[{expectancyColor}]${expectancy:N2}[/]",
            "Expected profit per trade");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Risk-Adjusted Returns
        AnsiConsole.MarkupLine("[bold underline]Risk-Adjusted Performance:[/]");
        AnsiConsole.WriteLine();

        var sharpeGrid = new Grid()
            .AddColumn(new GridColumn().Width(20))
            .AddColumn();

        sharpeGrid.AddRow(
            "[dim]Sharpe Ratio:[/]",
            FormatRatio(metrics.SharpeRatio, "Excellent", "Good", "Poor"));

        sharpeGrid.AddRow(
            "[dim]Sortino Ratio:[/]",
            FormatRatio(metrics.SortinoRatio, "Excellent", "Good", "Poor"));

        sharpeGrid.AddRow(
            "[dim]Calmar Ratio:[/]",
            FormatRatio(metrics.CalmarRatio, "Excellent", "Good", "Poor"));

        AnsiConsole.Write(sharpeGrid);

        return 0;
    }

    private static string FormatReturn(decimal returnPercent)
    {
        var color = returnPercent >= 0 ? "green" : "red";
        var sign = returnPercent >= 0 ? "+" : string.Empty;
        return $"[{color}]{sign}{returnPercent:F2}%[/]";
    }

    private static string FormatPercent(decimal percent)
    {
        var color = percent <= 10 ? "green" : percent <= 20 ? "yellow" : "red";
        return $"[{color}]{percent:F2}%[/]";
    }

    private static string FormatRatio(decimal ratio, string excellentLabel, string goodLabel, string poorLabel)
    {
        string color;
        string label;

        if (ratio >= 2.0m)
        {
            color = "green";
            label = excellentLabel;
        }
        else if (ratio >= 1.0m)
        {
            color = "yellow";
            label = goodLabel;
        }
        else
        {
            color = "red";
            label = poorLabel;
        }

        return $"[{color}]{ratio:F2}[/] ({label})";
    }
}
