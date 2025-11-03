// <copyright file="RiskSetStopLossCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Risk;

/// <summary>
/// Command to set default stop-loss percentage.
/// </summary>
public sealed class RiskSetStopLossCommand : AsyncCommand<RiskSetStopLossCommand.Settings>
{
    private readonly IRiskManager _riskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskSetStopLossCommand"/> class.
    /// </summary>
    /// <param name="riskManager">Risk manager.</param>
    public RiskSetStopLossCommand(IRiskManager riskManager)
    {
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            await _riskManager.SetStopLossAsync(settings.Percent);
            AnsiConsole.MarkupLine(
                $"[green]✓[/] Stop-loss set to [cyan]{settings.Percent:F1}%[/]");
            return 0;
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Settings for the risk set-stop-loss command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the stop-loss percentage.
        /// </summary>
        [CommandArgument(0, "<percent>")]
        [Description("Stop-loss percentage (0.1 - 20.0)")]
        public decimal Percent { get; set; }
    }
}
