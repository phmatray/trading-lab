// <copyright file="RiskSetMaxDrawdownCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Risk;

/// <summary>
/// Command to set maximum drawdown percentage.
/// </summary>
public sealed class RiskSetMaxDrawdownCommand : AsyncCommand<RiskSetMaxDrawdownCommand.Settings>
{
    private readonly IRiskManager _riskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskSetMaxDrawdownCommand"/> class.
    /// </summary>
    /// <param name="riskManager">Risk manager.</param>
    public RiskSetMaxDrawdownCommand(IRiskManager riskManager)
    {
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            await _riskManager.SetMaxDrawdownAsync(settings.Percent);
            AnsiConsole.MarkupLine(
                $"[green]✓[/] Max drawdown set to [cyan]{settings.Percent:F1}%[/]");
            return 0;
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Settings for the risk set-max-drawdown command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the maximum drawdown percentage.
        /// </summary>
        [CommandArgument(0, "<percent>")]
        [Description("Maximum drawdown percentage (1.0 - 50.0)")]
        public decimal Percent { get; set; }
    }
}
