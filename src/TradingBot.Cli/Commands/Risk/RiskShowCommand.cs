// <copyright file="RiskShowCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Risk;

/// <summary>
/// Command to display current risk settings.
/// </summary>
public sealed class RiskShowCommand : AsyncCommand
{
    private readonly IRiskManager _riskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskShowCommand"/> class.
    /// </summary>
    /// <param name="riskManager">Risk manager.</param>
    public RiskShowCommand(IRiskManager riskManager)
    {
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var settings = await _riskManager.GetRiskSettingsAsync();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Setting[/]")
            .AddColumn("[bold]Value[/]", c => c.RightAligned())
            .AddColumn("[bold]Description[/]");

        table.AddRow(
            "Leverage",
            $"{settings.Leverage:F1}x",
            "Account leverage multiplier");

        table.AddRow(
            "Stop-Loss",
            $"{settings.StopLossPercent:F1}%",
            "Default stop-loss percentage");

        table.AddRow(
            "Take-Profit",
            $"{settings.TakeProfitPercent:F1}%",
            "Default take-profit percentage");

        table.AddRow(
            "Daily Loss Limit",
            $"${settings.DailyLossLimit:N2}",
            "Maximum daily loss allowed");

        table.AddRow(
            "Max Drawdown",
            $"{settings.MaxDrawdownPercent:F1}%",
            "Maximum drawdown percentage");

        table.AddRow(
            "Max Position Size",
            $"{settings.MaxPositionSizePercent:F1}%",
            "Max position size as % of equity");

        var statusColor = settings.RiskLimitsEnabled ? "green" : "red";
        var statusText = settings.RiskLimitsEnabled ? "ENABLED" : "DISABLED";
        table.AddRow(
            "Risk Limits",
            $"[{statusColor}]{statusText}[/]",
            "Whether risk limits are enforced");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine(
            $"[dim]Last updated: {settings.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC[/]");

        return 0;
    }
}
