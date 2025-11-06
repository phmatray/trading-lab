// <copyright file="PerformanceWidget.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Rendering;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Dashboard.Widgets;

/// <summary>
/// Widget displaying performance metrics.
/// </summary>
public sealed class PerformanceWidget : IWidget
{
    private readonly IPortfolioManager _portfolioManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceWidget"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public PerformanceWidget(IPortfolioManager portfolioManager)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <inheritdoc/>
    public string Title => "Performance";

    /// <inheritdoc/>
    public async Task<IRenderable> RenderAsync(CancellationToken cancellationToken = default)
    {
        var metrics = await _portfolioManager.GetPerformanceMetricsAsync(cancellationToken);

        var returnColor = metrics.TotalReturn >= 0 ? "green" : "red";
        var returnSign = metrics.TotalReturn >= 0 ? "+" : string.Empty;
        var drawdownColor = metrics.MaxDrawdown <= 10 ? "green" : metrics.MaxDrawdown <= 20 ? "yellow" : "red";

        var grid = new Grid()
            .AddColumn()
            .AddColumn();

        grid.AddRow("[bold]Total Return:[/]", $"[{returnColor}]{returnSign}{metrics.TotalReturn:F2}%[/]");
        grid.AddRow("[bold]Win Rate:[/]", $"{metrics.WinRate:F1}%");
        grid.AddRow("[bold]Total Trades:[/]", metrics.TotalTrades.ToString());
        grid.AddRow("[bold]Winning/Losing:[/]", $"[green]{metrics.WinningTrades}[/] / [red]{metrics.LosingTrades}[/]");
        grid.AddRow("[bold]Sharpe Ratio:[/]", $"{metrics.SharpeRatio:F2}");
        grid.AddRow("[bold]Sortino Ratio:[/]", $"{metrics.SortinoRatio:F2}");
        grid.AddRow("[bold]Max Drawdown:[/]", $"[{drawdownColor}]{metrics.MaxDrawdown:F2}%[/]");
        grid.AddRow("[bold]Profit Factor:[/]", $"{metrics.ProfitFactor:F2}");

        return grid;
    }
}
