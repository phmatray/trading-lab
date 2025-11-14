// <copyright file="StrategyEnableCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Strategy;

/// <summary>
/// Command to enable a strategy.
/// </summary>
public sealed class StrategyEnableCommand : AsyncCommand<StrategyEnableCommand.Settings>
{
    private readonly IStrategyEngine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyEnableCommand"/> class.
    /// </summary>
    /// <param name="engine">Strategy engine.</param>
    public StrategyEnableCommand(IStrategyEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var success = await _engine.EnableStrategyAsync(settings.Name);

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Strategy [bold]{settings.Name}[/] enabled successfully.");
            return 0;
        }

        AnsiConsole.MarkupLine($"[red]Error:[/] Strategy [bold]{settings.Name}[/] not found.");
        AnsiConsole.MarkupLine("[dim]Use 'strategy list' to see available strategies.[/]");
        return 1;
    }

    /// <summary>
    /// Settings for the strategy enable command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the strategy name.
        /// </summary>
        [CommandArgument(0, "<NAME>")]
        [Description("Name of the strategy to enable")]
        public string Name { get; set; } = string.Empty;
    }
}
