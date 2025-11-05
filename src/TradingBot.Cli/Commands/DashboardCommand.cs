// <copyright file="DashboardCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands;

/// <summary>
/// Command to display the trading dashboard.
/// </summary>
public sealed class DashboardCommand : AsyncCommand
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly IRiskManager _riskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardCommand"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    /// <param name="riskManager">Risk manager.</param>
    public DashboardCommand(
        IPortfolioManager portfolioManager,
        IRiskManager riskManager)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        // Fetch all data
        var (account, positions, trades, metrics, riskSettings) = await AnsiConsole.Status()
            .StartAsync("Loading dashboard...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));

                var acc = await _portfolioManager.GetAccountAsync();
                var pos = await _portfolioManager.GetPositionsAsync();
                var trd = await _portfolioManager.GetTradeHistoryAsync();
                var met = await _portfolioManager.GetPerformanceMetricsAsync();
                var risk = await _riskManager.GetRiskSettingsAsync();

                return (acc, pos, trd, met, risk);
            });

        // Create dashboard title
        var rule = new Rule("[bold yellow]TradingBot Dashboard[/]")
        {
            Justification = Justify.Center,
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();

        // Create layout with 2 columns
        var layout = new Layout("Root")
            .SplitColumns(
                new Layout("Left"),
                new Layout("Right"));

        // Left column: Account & Performance
        var leftLayout = layout["Left"]
            .SplitRows(
                new Layout("Account"),
                new Layout("Performance"));

        // Account Panel
        var accountPanel = new Panel(new Markup(
            $"[bold]Account ID:[/] {account.AccountId}\n" +
            $"[bold]Equity:[/] [cyan]${account.Equity:N2}[/]\n" +
            $"[bold]Cash:[/] ${account.Cash:N2}\n" +
            $"[bold]Position Value:[/] ${account.PositionValue:N2}\n" +
            $"[bold]Buying Power:[/] ${account.BuyingPower:N2}\n" +
            $"[bold]Unrealized P&L:[/] {FormatPnL(account.UnrealizedPnL)}\n" +
            $"[bold]Realized P&L:[/] {FormatPnL(account.RealizedPnL)}"))
        {
            Header = new PanelHeader("[bold green]ACCOUNT[/]"),
            Border = BoxBorder.Rounded,
        };
        leftLayout["Account"].Update(accountPanel);

        // Performance Panel
        var performancePanel = new Panel(new Markup(
            $"[bold]Total Return:[/] {FormatReturn(metrics.TotalReturn)}\n" +
            $"[bold]Win Rate:[/] {metrics.WinRate:F1}%\n" +
            $"[bold]Total Trades:[/] {metrics.TotalTrades}\n" +
            $"[bold]Winning:[/] [green]{metrics.WinningTrades}[/] | [bold]Losing:[/] [red]{metrics.LosingTrades}[/]\n" +
            $"[bold]Sharpe Ratio:[/] {metrics.SharpeRatio:F2}\n" +
            $"[bold]Max Drawdown:[/] {FormatPercent(metrics.MaxDrawdown)}\n" +
            $"[bold]Profit Factor:[/] {metrics.ProfitFactor:F2}"))
        {
            Header = new PanelHeader("[bold yellow]PERFORMANCE[/]"),
            Border = BoxBorder.Rounded,
        };
        leftLayout["Performance"].Update(performancePanel);

        // Right column: Positions & Risk
        var rightLayout = layout["Right"]
            .SplitRows(
                new Layout("Positions"),
                new Layout("Risk"));

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

        // Risk Panel
        var riskStatusColor = riskSettings.RiskLimitsEnabled ? "green" : "red";
        var riskStatusText = riskSettings.RiskLimitsEnabled ? "ENABLED" : "DISABLED";
        var riskPanel = new Panel(new Markup(
            $"[bold]Leverage:[/] {riskSettings.Leverage:F1}x\n" +
            $"[bold]Stop-Loss:[/] {riskSettings.StopLossPercent:F1}%\n" +
            $"[bold]Take-Profit:[/] {riskSettings.TakeProfitPercent:F1}%\n" +
            $"[bold]Daily Loss Limit:[/] ${riskSettings.DailyLossLimit:N0}\n" +
            $"[bold]Max Drawdown:[/] {riskSettings.MaxDrawdownPercent:F1}%\n" +
            $"[bold]Status:[/] [{riskStatusColor}]{riskStatusText}[/]"))
        {
            Header = new PanelHeader("[bold red]RISK SETTINGS[/]"),
            Border = BoxBorder.Rounded,
        };
        rightLayout["Risk"].Update(riskPanel);

        // Render the layout
        AnsiConsole.Write(layout);
        AnsiConsole.WriteLine();

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

            AnsiConsole.Write(tradesTable);
        }
        else
        {
            AnsiConsole.MarkupLine("[dim]No trades yet[/]");
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
}
