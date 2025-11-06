// <copyright file="DashboardCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Cli.Dashboard;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands;

/// <summary>
/// Command to display the trading dashboard.
/// </summary>
public sealed class DashboardCommand : AsyncCommand<DashboardCommand.Settings>
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly IRiskManager _riskManager;
    private readonly DashboardRenderer _renderer;
    private readonly IAnsiConsole _console;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardCommand"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    /// <param name="riskManager">Risk manager.</param>
    /// <param name="renderer">Dashboard renderer.</param>
    /// <param name="console">Console for rendering output.</param>
    public DashboardCommand(
        IPortfolioManager portfolioManager,
        IRiskManager riskManager,
        DashboardRenderer renderer,
        IAnsiConsole console)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (settings.Live)
        {
            // Live mode with auto-refresh
            _console.MarkupLine("[yellow]Starting live dashboard...[/]");
            _console.MarkupLine($"[dim]Refresh interval: {settings.RefreshSeconds}s | Press Ctrl+C to exit[/]");
            _console.WriteLine();

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            try
            {
                await _renderer.StartAsync(TimeSpan.FromSeconds(settings.RefreshSeconds), cts.Token);
                return 0;
            }
            catch (OperationCanceledException)
            {
                _console.MarkupLine("\n[yellow]Dashboard stopped[/]");
                return 0;
            }
        }

        // Static mode (original implementation)
        var (account, positions, trades, metrics, riskSettings) = await _console.Status()
            .StartAsync("Loading dashboard...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));

                var acc = await _portfolioManager.GetAccountAsync(cancellationToken);
                var pos = await _portfolioManager.GetPositionsAsync(cancellationToken);
                var trd = await _portfolioManager.GetTradeHistoryAsync(cancellationToken: cancellationToken);
                var met = await _portfolioManager.GetPerformanceMetricsAsync(cancellationToken);
                var risk = await _riskManager.GetRiskSettingsAsync(cancellationToken);

                return (acc, pos, trd, met, risk);
            });

        // Create dashboard title
        var rule = new Rule("[bold yellow]TradingBot Dashboard[/]")
        {
            Justification = Justify.Center,
        };
        _console.Write(rule);
        _console.WriteLine();

        // Create layout with 2 columns (60/40 split for better proportions)
        var layout = new Layout("Root")
            .SplitColumns(
                new Layout("Left").Ratio(3),
                new Layout("Right").Ratio(2));

        // Left column: Account & Performance with minimum sizes
        var leftLayout = layout["Left"]
            .SplitRows(
                new Layout("Account").MinimumSize(10),
                new Layout("Performance").MinimumSize(10));

        // Account Panel with aligned grid
        var accountGrid = new Grid()
            .AddColumn(new GridColumn().Width(20).LeftAligned())
            .AddColumn(new GridColumn().NoWrap().RightAligned());

        accountGrid.AddRow("[bold]Account ID:[/]", $"{account.AccountId}");
        accountGrid.AddRow("[bold]Equity:[/]", $"[cyan]${account.Equity:N2}[/]");
        accountGrid.AddRow("[bold]Cash:[/]", $"${account.Cash:N2}");
        accountGrid.AddRow("[bold]Position Value:[/]", $"${account.PositionValue:N2}");
        accountGrid.AddRow("[bold]Buying Power:[/]", $"${account.BuyingPower:N2}");
        accountGrid.AddRow("[bold]Unrealized P&L:[/]", FormatPnL(account.UnrealizedPnL));
        accountGrid.AddRow("[bold]Realized P&L:[/]", FormatPnL(account.RealizedPnL));

        var accountPanel = new Panel(accountGrid)
        {
            Header = new PanelHeader("[bold green]ACCOUNT[/]"),
            Border = BoxBorder.Rounded,
        };
        accountPanel.Expand();
        leftLayout["Account"].Update(accountPanel);

        // Performance Panel with aligned grid
        var performanceGrid = new Grid()
            .AddColumn(new GridColumn().Width(20).LeftAligned())
            .AddColumn(new GridColumn().NoWrap().RightAligned());

        performanceGrid.AddRow("[bold]Total Return:[/]", FormatReturn(metrics.TotalReturn));
        performanceGrid.AddRow("[bold]Win Rate:[/]", $"{metrics.WinRate:F1}%");
        performanceGrid.AddRow("[bold]Total Trades:[/]", $"{metrics.TotalTrades}");
        performanceGrid.AddRow("[bold]Winning Trades:[/]", $"[green]{metrics.WinningTrades}[/]");
        performanceGrid.AddRow("[bold]Losing Trades:[/]", $"[red]{metrics.LosingTrades}[/]");
        performanceGrid.AddRow("[bold]Sharpe Ratio:[/]", $"{metrics.SharpeRatio:F2}");
        performanceGrid.AddRow("[bold]Max Drawdown:[/]", FormatPercent(metrics.MaxDrawdown));
        performanceGrid.AddRow("[bold]Profit Factor:[/]", $"{metrics.ProfitFactor:F2}");

        var performancePanel = new Panel(performanceGrid)
        {
            Header = new PanelHeader("[bold yellow]PERFORMANCE[/]"),
            Border = BoxBorder.Rounded,
        };
        performancePanel.Expand();
        leftLayout["Performance"].Update(performancePanel);

        // Right column: Positions & Risk with minimum sizes
        var rightLayout = layout["Right"]
            .SplitRows(
                new Layout("Positions").MinimumSize(10),
                new Layout("Risk").MinimumSize(10));

        // Positions Panel
        string positionsMarkup;
        if (positions.Count == 0)
        {
            positionsMarkup = "[dim]No open positions[/]";
        }
        else
        {
            var posLines = new List<string>();
            foreach (var position in positions.Take(5))
            {
                var pnlColor = position.UnrealizedPnL >= 0 ? "green" : "red";
                var pnlSign = position.UnrealizedPnL >= 0 ? "+" : string.Empty;
                posLines.Add(
                    $"[bold]{position.Symbol}[/] x{position.Quantity:F0} @ ${position.CurrentPrice:F2} " +
                    $"[{pnlColor}]{pnlSign}${position.UnrealizedPnL:N2}[/]");
            }

            if (positions.Count > 5)
            {
                posLines.Add($"[dim]... and {positions.Count - 5} more[/]");
            }

            positionsMarkup = string.Join("\n", posLines);
        }

        var positionsPanel = new Panel(new Markup(positionsMarkup))
        {
            Header = new PanelHeader("[bold cyan]POSITIONS[/]"),
            Border = BoxBorder.Rounded,
        };
        rightLayout["Positions"].Update(positionsPanel);

        // Risk Panel with aligned grid
        var riskStatusColor = riskSettings.RiskLimitsEnabled ? "green" : "red";
        var riskStatusText = riskSettings.RiskLimitsEnabled ? "ENABLED" : "DISABLED";

        var riskGrid = new Grid()
            .AddColumn(new GridColumn().Width(20).LeftAligned())
            .AddColumn(new GridColumn().NoWrap().RightAligned());

        riskGrid.AddRow("[bold]Leverage:[/]", $"{riskSettings.Leverage:F1}x");
        riskGrid.AddRow("[bold]Stop-Loss:[/]", $"{riskSettings.StopLossPercent:F1}%");
        riskGrid.AddRow("[bold]Take-Profit:[/]", $"{riskSettings.TakeProfitPercent:F1}%");
        riskGrid.AddRow("[bold]Daily Loss Limit:[/]", $"${riskSettings.DailyLossLimit:N0}");
        riskGrid.AddRow("[bold]Max Drawdown:[/]", $"{riskSettings.MaxDrawdownPercent:F1}%");
        riskGrid.AddRow("[bold]Status:[/]", $"[{riskStatusColor}]{riskStatusText}[/]");

        var riskPanel = new Panel(riskGrid)
        {
            Header = new PanelHeader("[bold red]RISK SETTINGS[/]"),
            Border = BoxBorder.Rounded,
        };
        riskPanel.Expand();
        rightLayout["Risk"].Update(riskPanel);

        // Render the layout
        _console.Write(layout);
        _console.WriteLine();

        // Recent Trades (bottom section)
        if (trades.Count > 0)
        {
            var tradesTable = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Recent Trades[/]")
                .AddColumn("[bold]Date[/]")
                .AddColumn("[bold]Symbol[/]")
                .AddColumn("[bold]Side[/]", c => c.Centered())
                .AddColumn("[bold]Qty[/]", c => c.RightAligned())
                .AddColumn("[bold]P&L[/]", c => c.RightAligned());

            foreach (var trade in trades.Take(5))
            {
                var sideColor = trade.Side.ToString() == "Buy" ? "green" : "red";
                var pnlColor = trade.RealizedPnL >= 0 ? "green" : "red";
                var pnlSign = trade.RealizedPnL >= 0 ? "+" : string.Empty;

                tradesTable.AddRow(
                    trade.ExitTime.ToString("MM/dd HH:mm"),
                    trade.Symbol,
                    $"[{sideColor}]{trade.Side}[/]",
                    trade.Quantity.ToString("F0"),
                    $"[{pnlColor}]{pnlSign}${trade.RealizedPnL:N2}[/]");
            }

            _console.Write(tradesTable);
        }
        else
        {
            _console.MarkupLine("[dim]No trades yet[/]");
        }

        return 0;
    }

    private static string FormatPnL(decimal pnl)
    {
        var color = pnl >= 0 ? "green" : "red";
        var sign = pnl >= 0 ? "+" : string.Empty;
        return $"[{color}]{sign}${pnl:N2}[/]";
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

    /// <summary>
    /// Dashboard command settings.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the refresh interval in seconds.
        /// </summary>
        [Description("Refresh interval in seconds")]
        [CommandOption("--refresh")]
        [DefaultValue(2)]
        public int RefreshSeconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use live mode.
        /// </summary>
        [Description("Enable live updates")]
        [CommandOption("--live")]
        [DefaultValue(true)]
        public bool Live { get; set; }
    }
}
