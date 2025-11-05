// <copyright file="StrategyStopCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Strategy;

/// <summary>
/// Command to stop the strategy engine.
/// </summary>
public sealed class StrategyStopCommand : AsyncCommand
{
    private readonly IStrategyEngine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyStopCommand"/> class.
    /// </summary>
    /// <param name="engine">Strategy engine.</param>
    public StrategyStopCommand(IStrategyEngine engine)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellationToken)
    {
        if (!_engine.IsRunning)
        {
            AnsiConsole.MarkupLine("[yellow]Strategy engine is not currently running.[/]");
            return 0;
        }

        await AnsiConsole.Status()
            .StartAsync("Stopping strategy engine...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("yellow"));

                await _engine.StopAsync();
            });

        AnsiConsole.MarkupLine("[green]✓[/] Strategy engine stopped successfully.");

        return 0;
    }
}
