// <copyright file="RiskSetLeverageCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Risk;

/// <summary>
/// Command to set account leverage.
/// </summary>
public sealed class RiskSetLeverageCommand : AsyncCommand<RiskSetLeverageCommand.Settings>
{
    private readonly IRiskManager _riskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskSetLeverageCommand"/> class.
    /// </summary>
    /// <param name="riskManager">Risk manager.</param>
    public RiskSetLeverageCommand(IRiskManager riskManager)
    {
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            await _riskManager.SetLeverageAsync(settings.Leverage);
            AnsiConsole.MarkupLine(
                $"[green]✓[/] Leverage set to [cyan]{settings.Leverage:F1}x[/]");
            return 0;
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Settings for the risk set-leverage command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the leverage multiplier.
        /// </summary>
        [CommandArgument(0, "<leverage>")]
        [Description("Leverage multiplier (1.0 - 10.0)")]
        public decimal Leverage { get; set; }
    }
}
