// <copyright file="StrategyStartCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Strategy;

/// <summary>
/// Command to start the strategy engine.
/// </summary>
public sealed class StrategyStartCommand : AsyncCommand<StrategyStartCommand.Settings>
{
    private readonly IStrategyEngine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyStartCommand"/> class.
    /// </summary>
    /// <param name="engine">Strategy engine.</param>
    public StrategyStartCommand(IStrategyEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (_engine.IsRunning)
        {
            AnsiConsole.MarkupLine("[yellow]Strategy engine is already running.[/]");
            return 0;
        }

        var strategies = await _engine.GetStrategiesAsync(cancellationToken);
        var enabledStrategies = strategies.Where(s => s.IsEnabled).ToList();

        if (enabledStrategies.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No strategies are enabled.");
            AnsiConsole.MarkupLine("[dim]Enable at least one strategy using 'strategy enable <name>' before starting the engine.[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[green]Starting strategy engine...[/]");
        AnsiConsole.MarkupLine($"[dim]Execution interval: {settings.Interval} seconds[/]");
        AnsiConsole.MarkupLine($"[dim]Enabled strategies: {string.Join(", ", enabledStrategies.Select(s => s.Name))}[/]");
        AnsiConsole.WriteLine();

        var interval = TimeSpan.FromSeconds(settings.Interval);
        await _engine.StartAsync(interval, cancellationToken);

        AnsiConsole.MarkupLine("[green]✓[/] Strategy engine started successfully.");
        AnsiConsole.MarkupLine("[dim]The engine will execute strategies every {0} seconds.[/]", settings.Interval);
        AnsiConsole.MarkupLine("[dim]Use 'strategy stop' to stop the engine.[/]");

        return 0;
    }

    /// <summary>
    /// Settings for the strategy start command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the execution interval in seconds.
        /// </summary>
        [CommandOption("--interval|-i")]
        [Description("Execution interval in seconds")]
        [DefaultValue(60)]
        public int Interval { get; set; } = 60;
    }
}
