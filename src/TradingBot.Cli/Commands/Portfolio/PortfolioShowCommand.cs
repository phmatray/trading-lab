// <copyright file="PortfolioShowCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Portfolio;

/// <summary>
/// Command to display current portfolio positions.
/// </summary>
public sealed class PortfolioShowCommand : AsyncCommand
{
    private readonly IPortfolioManager _portfolioManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PortfolioShowCommand"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public PortfolioShowCommand(IPortfolioManager portfolioManager)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var (account, positions) = await AnsiConsole.Status()
            .StartAsync("Loading portfolio...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);
                ctx.SpinnerStyle(Style.Parse("green"));

                var acc = await _portfolioManager.GetAccountAsync();
                var pos = await _portfolioManager.GetPositionsAsync();
                return (acc, pos);
            });

        // Account Summary
        var accountPanel = new Panel(new Markup(
            $"[bold]Account ID:[/] {account.AccountId}\n" +
            $"[bold]Equity:[/] [cyan]${account.Equity:N2}[/]\n" +
            $"[bold]Cash:[/] ${account.Cash:N2}\n" +
            $"[bold]Position Value:[/] ${account.PositionValue:N2}\n" +
            $"[bold]Buying Power:[/] ${account.BuyingPower:N2}\n" +
            $"[bold]Unrealized P&L:[/] {FormatPnL(account.UnrealizedPnL)}\n" +
            $"[bold]Realized P&L:[/] {FormatPnL(account.RealizedPnL)}"))
        {
            Header = new PanelHeader("[bold yellow]ACCOUNT SUMMARY[/]"),
            Border = BoxBorder.Rounded,
        };

        AnsiConsole.Write(accountPanel);
        AnsiConsole.WriteLine();

        // Positions Table
        if (positions.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No open positions.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("[bold]Symbol[/]")
            .AddColumn("[bold]Side[/]", c => c.Centered())
            .AddColumn("[bold]Quantity[/]", c => c.RightAligned())
            .AddColumn("[bold]Entry Price[/]", c => c.RightAligned())
            .AddColumn("[bold]Current Price[/]", c => c.RightAligned())
            .AddColumn("[bold]P&L[/]", c => c.RightAligned())
            .AddColumn("[bold]Strategy[/]");

        foreach (var position in positions.OrderBy(p => p.Symbol))
        {
            var sideMarkup = position.Side.ToString() == "Buy"
                ? "[green]BUY[/]"
                : "[red]SELL[/]";

            var pnlPercent = position.EntryPrice > 0
                ? ((position.CurrentPrice - position.EntryPrice) / position.EntryPrice) * 100m
                : 0m;

            table.AddRow(
                position.Symbol,
                sideMarkup,
                position.Quantity.ToString("F2"),
                $"${position.EntryPrice:F2}",
                $"${position.CurrentPrice:F2}",
                $"{FormatPnL(position.UnrealizedPnL)} ({FormatPnLPercent(pnlPercent)})",
                position.StrategyName);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        // Summary
        var totalPnL = positions.Sum(p => p.UnrealizedPnL);
        AnsiConsole.MarkupLine($"[dim]Total Unrealized P&L: {FormatPnL(totalPnL)}[/]");

        return 0;
    }

    private static string FormatPnL(decimal pnl)
    {
        var color = pnl >= 0 ? "green" : "red";
        var sign = pnl >= 0 ? "+" : string.Empty;
        return $"[{color}]{sign}${pnl:N2}[/]";
    }

    private static string FormatPnLPercent(decimal percent)
    {
        var color = percent >= 0 ? "green" : "red";
        var sign = percent >= 0 ? "+" : string.Empty;
        return $"[{color}]{sign}{percent:F2}%[/]";
    }
}
