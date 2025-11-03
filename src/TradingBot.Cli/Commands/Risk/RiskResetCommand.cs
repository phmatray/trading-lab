// <copyright file="RiskResetCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Risk;

/// <summary>
/// Command to reset risk settings to defaults.
/// </summary>
public sealed class RiskResetCommand : AsyncCommand
{
    private readonly IRiskManager _riskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskResetCommand"/> class.
    /// </summary>
    /// <param name="riskManager">Risk manager.</param>
    public RiskResetCommand(IRiskManager riskManager)
    {
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        // Confirm before resetting
        if (!AnsiConsole.Confirm("Reset [bold red]ALL[/] risk settings to defaults?", defaultValue: false))
        {
            AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
            return 0;
        }

        await _riskManager.ResetToDefaultsAsync();

        AnsiConsole.MarkupLine("[green]✓[/] Risk settings reset to defaults");
        AnsiConsole.WriteLine();

        // Show the default settings
        var settings = await _riskManager.GetRiskSettingsAsync();

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Setting[/]")
            .AddColumn("[bold]Default Value[/]", c => c.RightAligned());

        table.AddRow("Leverage", $"{settings.Leverage:F1}x");
        table.AddRow("Stop-Loss", $"{settings.StopLossPercent:F1}%");
        table.AddRow("Take-Profit", $"{settings.TakeProfitPercent:F1}%");
        table.AddRow("Daily Loss Limit", $"${settings.DailyLossLimit:N2}");
        table.AddRow("Max Drawdown", $"{settings.MaxDrawdownPercent:F1}%");
        table.AddRow("Max Position Size", $"{settings.MaxPositionSizePercent:F1}%");

        AnsiConsole.Write(table);

        return 0;
    }
}
