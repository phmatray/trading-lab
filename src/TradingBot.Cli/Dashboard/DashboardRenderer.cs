// <copyright file="DashboardRenderer.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Spectre.Console;
using TradingBot.Cli.Dashboard.Widgets;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Dashboard;

/// <summary>
/// Renders the live trading dashboard with real-time updates.
/// </summary>
public sealed class DashboardRenderer
{
    private readonly IPortfolioManager _portfolioManager;
    private readonly IRiskManager _riskManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardRenderer"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    /// <param name="riskManager">Risk manager.</param>
    public DashboardRenderer(
        IPortfolioManager portfolioManager,
        IRiskManager riskManager)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
        _riskManager = riskManager ?? throw new ArgumentNullException(nameof(riskManager));
    }

    /// <summary>
    /// Starts the live dashboard with the specified refresh interval.
    /// </summary>
    /// <param name="refreshInterval">Time between updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync(TimeSpan refreshInterval, CancellationToken cancellationToken = default)
    {
        AnsiConsole.Clear();

        await AnsiConsole.Live(CreateInitialLayout())
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .Cropping(VerticalOverflowCropping.Top)
            .StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        var layout = await UpdateLayoutAsync(cancellationToken);
                        ctx.UpdateTarget(layout);
                        await Task.Delay(refreshInterval, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error updating dashboard: {ex.Message}[/]");
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
            });
    }

    private static Layout CreateInitialLayout()
    {
        return new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(3),
                new Layout("Body"),
                new Layout("Footer").Size(1));
    }

    private static async Task<Panel> CreatePanelAsync(
        IWidget widget,
        Color borderColor,
        CancellationToken cancellationToken)
    {
        var content = await widget.RenderAsync(cancellationToken);

        return new Panel(content)
        {
            Header = new PanelHeader($"[bold]{widget.Title}[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(borderColor),
        };
    }

    private async Task<Layout> UpdateLayoutAsync(CancellationToken cancellationToken)
    {
        // Create widgets
        var accountWidget = new AccountWidget(_portfolioManager);
        var positionsWidget = new PositionsWidget(_portfolioManager);
        var performanceWidget = new PerformanceWidget(_portfolioManager);
        var riskWidget = new RiskWidget(_riskManager);
        var tradesWidget = new RecentTradesWidget(_portfolioManager, maxTrades: 5);

        // Render widgets
        var accountPanel = await CreatePanelAsync(accountWidget, Color.Green, cancellationToken);
        var positionsPanel = await CreatePanelAsync(positionsWidget, Color.Cyan1, cancellationToken);
        var performancePanel = await CreatePanelAsync(performanceWidget, Color.Yellow3, cancellationToken);
        var riskPanel = await CreatePanelAsync(riskWidget, Color.Red, cancellationToken);
        var tradesPanel = await CreatePanelAsync(tradesWidget, Color.Blue, cancellationToken);

        // Create layout
        var layout = new Layout("Root")
            .SplitRows(
                new Layout("Header").Size(3),
                new Layout("Body"),
                new Layout("Footer").Size(1));

        // Header
        var title = new FigletText("TradingBot")
            .Centered()
            .Color(Color.Yellow);
        layout["Header"].Update(title);

        // Body with 2 columns
        var body = layout["Body"]
            .SplitColumns(
                new Layout("Left"),
                new Layout("Right"));

        // Left column: Account, Performance, Risk
        body["Left"]
            .SplitRows(
                new Layout("Account").Size(10),
                new Layout("Performance").Size(12),
                new Layout("Risk").Size(10));

        body["Left"]["Account"].Update(accountPanel);
        body["Left"]["Performance"].Update(performancePanel);
        body["Left"]["Risk"].Update(riskPanel);

        // Right column: Positions and Trades
        body["Right"]
            .SplitRows(
                new Layout("Positions"),
                new Layout("Trades"));

        body["Right"]["Positions"].Update(positionsPanel);
        body["Right"]["Trades"].Update(tradesPanel);

        // Footer
        var footer = new Markup($"[dim]Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Press Ctrl+C to exit[/]")
            .Centered();
        layout["Footer"].Update(footer);

        return layout;
    }
}
