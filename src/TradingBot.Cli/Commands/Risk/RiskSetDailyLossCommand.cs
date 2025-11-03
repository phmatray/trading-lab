// <copyright file="RiskSetDailyLossCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Risk;

/// <summary>
/// Command to set maximum daily loss limit.
/// </summary>
public sealed class RiskSetDailyLossCommand : AsyncCommand<RiskSetDailyLossCommand.Settings>
{
    private readonly IRiskManager _riskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskSetDailyLossCommand"/> class.
    /// </summary>
    /// <param name="riskManager">Risk manager.</param>
    public RiskSetDailyLossCommand(IRiskManager riskManager)
    {
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            await _riskManager.SetDailyLossLimitAsync(settings.Amount);
            AnsiConsole.MarkupLine(
                $"[green]✓[/] Daily loss limit set to [cyan]${settings.Amount:N2}[/]");
            return 0;
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Settings for the risk set-daily-loss command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the daily loss limit amount.
        /// </summary>
        [CommandArgument(0, "<amount>")]
        [Description("Maximum daily loss amount in dollars")]
        public decimal Amount { get; set; }
    }
}
