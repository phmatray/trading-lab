// <copyright file="BacktestRunCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Backtest;

namespace TradingBot.Cli.Commands.Backtest;

/// <summary>
/// Command to run a backtest.
/// </summary>
public sealed class BacktestRunCommand : AsyncCommand<BacktestRunCommand.Settings>
{
    private readonly IBacktestingEngine _backtestingEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="BacktestRunCommand"/> class.
    /// </summary>
    /// <param name="backtestingEngine">Backtesting engine instance.</param>
    public BacktestRunCommand(IBacktestingEngine backtestingEngine)
    {
        _backtestingEngine = backtestingEngine ?? throw new ArgumentNullException(nameof(backtestingEngine));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.Write(
            new FigletText("Backtest Run")
                .Color(Color.Yellow));

        // Parse dates
        if (!DateTime.TryParse(settings.StartDate, out var startDate))
        {
            AnsiConsole.MarkupLine($"[red]Error: Invalid start date '{settings.StartDate}'[/]");
            return 1;
        }

        if (!DateTime.TryParse(settings.EndDate, out var endDate))
        {
            AnsiConsole.MarkupLine($"[red]Error: Invalid end date '{settings.EndDate}'[/]");
            return 1;
        }

        if (endDate <= startDate)
        {
            AnsiConsole.MarkupLine("[red]Error: End date must be after start date[/]");
            return 1;
        }

        // Display configuration
        var configTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey);

        configTable.AddColumn("[yellow]Parameter[/]");
        configTable.AddColumn("[yellow]Value[/]");

        configTable.AddRow("Strategy", $"[cyan]{settings.Strategy}[/]");
        configTable.AddRow("Symbol", $"[cyan]{settings.Symbol}[/]");
        configTable.AddRow("Start Date", $"[cyan]{startDate:yyyy-MM-dd}[/]");
        configTable.AddRow("End Date", $"[cyan]{endDate:yyyy-MM-dd}[/]");
        configTable.AddRow("Initial Capital", $"[green]${settings.InitialCapital:N2}[/]");

        AnsiConsole.Write(configTable);
        AnsiConsole.WriteLine();

        // Create backtest configuration
        var configuration = new BacktestConfiguration
        {
            BacktestId = Guid.NewGuid().ToString(),
            StrategyName = settings.Strategy,
            Symbol = settings.Symbol,
            StartDate = startDate,
            EndDate = endDate,
            InitialCapital = settings.InitialCapital,
            CommissionPerTrade = 1.0m,
            EnableTransactionCosts = true,
        };

        // Run backtest with progress
        BacktestResult? result = null;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("yellow"))
            .StartAsync("Running backtest...", async ctx =>
            {
                result = await _backtestingEngine.RunBacktestAsync(configuration);
            });

        if (result == null)
        {
            AnsiConsole.MarkupLine("[red]Error: Backtest failed to produce results[/]");
            return 1;
        }

        // Display results summary
        AnsiConsole.WriteLine();
        DisplayBacktestResults(result);

        return 0;
    }

    private static void DisplayBacktestResults(BacktestResult result)
    {
        var panel = new Panel(
            new Markup($"[green]Backtest completed successfully![/]\n" +
                       $"[grey]Backtest ID:[/] [cyan]{result.BacktestId}[/]\n" +
                       $"[grey]Duration:[/] [cyan]{result.Duration.TotalSeconds:F2}s[/]"))
        {
            Header = new PanelHeader("[yellow]Backtest Complete[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow),
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();

        // Performance Summary
        var perfTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Green)
            .AddColumn("[yellow]Metric[/]")
            .AddColumn("[yellow]Value[/]");

        perfTable.AddRow("Initial Capital", $"[cyan]${result.InitialCapital:N2}[/]");
        perfTable.AddRow("Final Equity", $"[cyan]${result.FinalEquity:N2}[/]");
        perfTable.AddRow("Total P&L", FormatPnL(result.TotalPnL));
        perfTable.AddRow("Total Return", FormatReturn(result.TotalReturn));
        perfTable.AddRow("Total Trades", $"[cyan]{result.Trades.Count}[/]");

        AnsiConsole.Write(perfTable);
        AnsiConsole.WriteLine();

        // Trading Statistics
        var statsTable = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Blue)
            .AddColumn("[yellow]Statistic[/]")
            .AddColumn("[yellow]Value[/]");

        var perf = result.Performance;
        statsTable.AddRow("Winning Trades", $"[green]{perf.WinningTrades}[/]");
        statsTable.AddRow("Losing Trades", $"[red]{perf.LosingTrades}[/]");
        statsTable.AddRow("Win Rate", $"[cyan]{perf.WinRate:F2}%[/]");
        statsTable.AddRow("Profit Factor", $"[cyan]{perf.ProfitFactor:F2}[/]");
        statsTable.AddRow("Average Win", $"[green]${perf.AverageWin:N2}[/]");
        statsTable.AddRow("Average Loss", $"[red]${perf.AverageLoss:N2}[/]");

        AnsiConsole.Write(statsTable);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"[grey]View full report with:[/] [cyan]tradingbot backtest report {result.BacktestId}[/]");
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
    /// Settings for the backtest run command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the strategy name.
        /// </summary>
        [CommandArgument(0, "<strategy>")]
        [Description("Strategy name to backtest")]
        public string Strategy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the symbol.
        /// </summary>
        [CommandOption("--symbol|-s")]
        [Description("Symbol to backtest (e.g., SPY)")]
        [DefaultValue("SPY")]
        public string Symbol { get; set; } = "SPY";

        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        [CommandOption("--start-date")]
        [Description("Start date (YYYY-MM-DD)")]
        [DefaultValue("2024-01-01")]
        public string StartDate { get; set; } = "2024-01-01";

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        [CommandOption("--end-date")]
        [Description("End date (YYYY-MM-DD)")]
        [DefaultValue("2024-12-31")]
        public string EndDate { get; set; } = "2024-12-31";

        /// <summary>
        /// Gets or sets the initial capital.
        /// </summary>
        [CommandOption("--capital")]
        [Description("Initial capital amount")]
        [DefaultValue(100000)]
        public decimal InitialCapital { get; set; } = 100000m;
    }
}
