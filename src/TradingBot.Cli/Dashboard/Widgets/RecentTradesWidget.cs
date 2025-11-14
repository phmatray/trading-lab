// <copyright file="RecentTradesWidget.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Rendering;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Dashboard.Widgets;

/// <summary>
/// Widget displaying recent closed trades.
/// </summary>
public sealed class RecentTradesWidget : IWidget
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly int _maxTrades;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentTradesWidget"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    /// <param name="maxTrades">Maximum number of trades to display.</param>
    public RecentTradesWidget(IPortfolioManager portfolioManager, int maxTrades = 10)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
        _maxTrades = maxTrades;
    }

    /// <inheritdoc/>
    public string Title => "Recent Trades";

    /// <inheritdoc/>
    public async Task<IRenderable> RenderAsync(CancellationToken cancellationToken = default)
    {
        var allTrades = await _portfolioManager.GetTradeHistoryAsync(cancellationToken: cancellationToken);
        var trades = allTrades.Take(_maxTrades).ToList();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Yellow3)
            .AddColumn(new TableColumn("[bold]Date[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Symbol[/]").Centered())
            .AddColumn(new TableColumn("[bold]Side[/]").Centered())
            .AddColumn(new TableColumn("[bold]Qty[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Entry[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Exit[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]P&L[/]").RightAligned());

        if (trades.Count == 0)
        {
            table.AddRow(
                "[dim]No trades yet[/]",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }
        else
        {
            foreach (var trade in trades)
            {
                var sideColor = trade.Side.ToString() == "Buy" ? "green" : "red";
                var pnlColor = trade.RealizedPnL >= 0 ? "green" : "red";
                var pnlSign = trade.RealizedPnL >= 0 ? "+" : string.Empty;

                table.AddRow(
                    trade.ExitTime.ToString("MM/dd HH:mm"),
                    trade.Symbol,
                    $"[{sideColor}]{trade.Side}[/]",
                    trade.Quantity.ToString("F2"),
                    $"${trade.EntryPrice:F2}",
                    $"${trade.ExitPrice:F2}",
                    $"[{pnlColor}]{pnlSign}${trade.RealizedPnL:N2}[/]");
            }
        }

        return table;
    }
}
