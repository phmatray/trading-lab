// <copyright file="RiskWidget.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Rendering;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Dashboard.Widgets;

/// <summary>
/// Widget displaying risk management settings.
/// </summary>
public sealed class RiskWidget : IWidget
{
    private readonly IRiskManager _riskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskWidget"/> class.
    /// </summary>
    /// <param name="riskManager">Risk manager.</param>
    public RiskWidget(IRiskManager riskManager)
    {
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
    }

    /// <inheritdoc/>
    public string Title => "Risk Settings";

    /// <inheritdoc/>
    public async Task<IRenderable> RenderAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _riskManager.GetRiskSettingsAsync(cancellationToken);

        var statusColor = settings.RiskLimitsEnabled ? "green" : "red";
        var statusText = settings.RiskLimitsEnabled ? "ENABLED" : "DISABLED";

        var grid = new Grid()
            .AddColumn(new GridColumn().Width(20).LeftAligned())
            .AddColumn(new GridColumn().NoWrap().RightAligned());

        grid.AddRow("[bold]Status:[/]", $"[{statusColor}]{statusText}[/]");
        grid.AddRow("[bold]Leverage:[/]", $"{settings.Leverage:F1}x");
        grid.AddRow("[bold]Stop-Loss:[/]", $"{settings.StopLossPercent:F1}%");
        grid.AddRow("[bold]Take-Profit:[/]", $"{settings.TakeProfitPercent:F1}%");
        grid.AddRow("[bold]Daily Loss Limit:[/]", $"${settings.DailyLossLimit:N0}");
        grid.AddRow("[bold]Max Drawdown:[/]", $"{settings.MaxDrawdownPercent:F1}%");
        grid.AddRow("[bold]Max Position Size:[/]", $"{settings.MaxPositionSizePercent:F1}%");

        return grid;
    }
}
