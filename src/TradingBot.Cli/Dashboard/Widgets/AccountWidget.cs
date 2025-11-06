// <copyright file="AccountWidget.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using Spectre.Console.Rendering;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Dashboard.Widgets;

/// <summary>
/// Widget displaying account information and equity.
/// </summary>
public sealed class AccountWidget : IWidget
{
    private readonly IPortfolioManager _portfolioManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountWidget"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public AccountWidget(IPortfolioManager portfolioManager)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <inheritdoc/>
    public string Title => "Account";

    /// <inheritdoc/>
    public async Task<IRenderable> RenderAsync(CancellationToken cancellationToken = default)
    {
        var account = await _portfolioManager.GetAccountAsync(cancellationToken);

        var unrealizedColor = account.UnrealizedPnL >= 0 ? "green" : "red";
        var unrealizedSign = account.UnrealizedPnL >= 0 ? "+" : string.Empty;
        var realizedColor = account.RealizedPnL >= 0 ? "green" : "red";
        var realizedSign = account.RealizedPnL >= 0 ? "+" : string.Empty;

        var grid = new Grid()
            .AddColumn(new GridColumn().Width(18).LeftAligned())
            .AddColumn(new GridColumn().NoWrap().RightAligned());

        grid.AddRow("[bold]Account ID:[/]", $"{account.AccountId}");
        grid.AddRow("[bold]Equity:[/]", $"[cyan]${account.Equity:N2}[/]");
        grid.AddRow("[bold]Cash:[/]", $"${account.Cash:N2}");
        grid.AddRow("[bold]Position Value:[/]", $"${account.PositionValue:N2}");
        grid.AddRow("[bold]Buying Power:[/]", $"${account.BuyingPower:N2}");
        grid.AddRow("[bold]Unrealized P&L:[/]", $"[{unrealizedColor}]{unrealizedSign}${account.UnrealizedPnL:N2}[/]");
        grid.AddRow("[bold]Realized P&L:[/]", $"[{realizedColor}]{realizedSign}${account.RealizedPnL:N2}[/]");

        return grid;
    }
}
