// <copyright file="PositionsWidget.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Rendering;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Dashboard.Widgets;

/// <summary>
/// Widget displaying current open positions.
/// </summary>
public sealed class PositionsWidget : IWidget
{
    private readonly IPortfolioManager _portfolioManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PositionsWidget"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public PositionsWidget(IPortfolioManager portfolioManager)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <inheritdoc/>
    public string Title => "Open Positions";

    /// <inheritdoc/>
    public async Task<IRenderable> RenderAsync(CancellationToken cancellationToken = default)
    {
        var positions = await _portfolioManager.GetPositionsAsync(cancellationToken);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Cyan1)
            .AddColumn(new TableColumn("[bold]Symbol[/]").Centered())
            .AddColumn(new TableColumn("[bold]Qty[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Entry[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]Current[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]P&L[/]").RightAligned())
            .AddColumn(new TableColumn("[bold]%[/]").RightAligned());

        if (positions.Count == 0)
        {
            table.AddRow("[dim]No open positions[/]", string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
        }
        else
        {
            foreach (var position in positions.OrderByDescending(p => Math.Abs(p.UnrealizedPnL)))
            {
                var pnlColor = position.UnrealizedPnL >= 0 ? "green" : "red";
                var pnlSign = position.UnrealizedPnL >= 0 ? "+" : string.Empty;
                var pnlPercentSign = position.UnrealizedPnLPercent >= 0 ? "+" : string.Empty;

                table.AddRow(
                    $"[bold]{position.Symbol}[/]",
                    position.Quantity.ToString("F2"),
                    $"${position.EntryPrice:F2}",
                    $"${position.CurrentPrice:F2}",
                    $"[{pnlColor}]{pnlSign}${position.UnrealizedPnL:N2}[/]",
                    $"[{pnlColor}]{pnlPercentSign}{position.UnrealizedPnLPercent:F2}%[/]");
            }
        }

        return table;
    }
}
