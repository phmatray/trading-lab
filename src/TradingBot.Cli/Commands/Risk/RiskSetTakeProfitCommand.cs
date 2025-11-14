// <copyright file="RiskSetTakeProfitCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Risk;

/// <summary>
/// Command to set default take-profit percentage.
/// </summary>
public sealed class RiskSetTakeProfitCommand : AsyncCommand<RiskSetTakeProfitCommand.Settings>
{
    private readonly IRiskManager _riskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskSetTakeProfitCommand"/> class.
    /// </summary>
    /// <param name="riskManager">Risk manager.</param>
    public RiskSetTakeProfitCommand(IRiskManager riskManager)
    {
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        try
        {
            await _riskManager.SetTakeProfitAsync(settings.Percent);
            AnsiConsole.MarkupLine(
                $"[green]✓[/] Take-profit set to [cyan]{settings.Percent:F1}%[/]");
            return 0;
        }
        catch (ArgumentException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Settings for the risk set-take-profit command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the take-profit percentage.
        /// </summary>
        [CommandArgument(0, "<percent>")]
        [Description("Take-profit percentage (0.1 - 50.0)")]
        public decimal Percent { get; set; }
    }
}
