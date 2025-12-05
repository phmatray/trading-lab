using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using TradingStrat.Data;
using TradingStrat.Models;
using TradingStrat.Services;
using TradingStrat.Services.Backtesting;
using TradingStrat.Services.Strategies;
using TradingStrat.Utilities;

namespace TradingStrat;

public static class ProgramBacktest
{
    public static async Task RunAsync()
    {
        AnsiConsole.Write(
            new FigletText("Trading Strategy")
                .LeftJustified()
                .Color(Color.Cyan1));

        AnsiConsole.MarkupLine("[grey]Backtesting System powered by Yahoo Finance[/]\n");

        try
        {
            TradingContext context = null!;

            await AnsiConsole.Status()
                .StartAsync("Initializing database...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("green"));

                    context = new TradingContext();
                    await context.Database.EnsureCreatedAsync();

                    ctx.Status("[green]Database initialized successfully[/]");
                    await Task.Delay(300);
                });

            AnsiConsole.MarkupLine("[green]✓[/] Database ready\n");

            var repository = new DataRepository(context);
            var exportService = new ExportService();
            var backtestEngine = new BacktestEngine(repository);
            var presenter = new BacktestPresenter();

            const string ticker = "CON3.L";

            var dataCount = await context.HistoricalPrices.CountAsync(p => p.Ticker == ticker);
            if (dataCount == 0)
            {
                AnsiConsole.MarkupLine($"[red]✗ ERROR:[/] No historical data found for {ticker}");
                AnsiConsole.MarkupLine("[yellow]Please run the data fetcher first to download historical data[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]✓[/] Found {dataCount:N0} historical price records for {ticker}\n");

            var strategy = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Select trading strategy:[/]")
                    .PageSize(10)
                    .AddChoices(
                        "MA Crossover (20/50)",
                        "MA Crossover (10/30)",
                        "RSI (14, 30/70)",
                        "MACD (12/26/9)"));

            IStrategy selectedStrategy = strategy switch
            {
                "MA Crossover (20/50)" => new MovingAverageCrossoverStrategy(20, 50),
                "MA Crossover (10/30)" => new MovingAverageCrossoverStrategy(10, 30),
                "RSI (14, 30/70)" => new RSIStrategy(14, 30, 70),
                "MACD (12/26/9)" => new MACDStrategy(12, 26, 9),
                _ => new MovingAverageCrossoverStrategy(20, 50)
            };

            var initialCapitalInput = AnsiConsole.Ask<decimal>("[yellow]Initial capital:[/]", 10_000m);

            var latestDate = await repository.GetLatestDataDateAsync(ticker);
            var oldestDate = await context.HistoricalPrices
                .Where(p => p.Ticker == ticker)
                .MinAsync(p => (DateTime?)p.DateTime);

            var startDate = oldestDate ?? new DateTime(2021, 12, 10);
            var endDate = latestDate ?? DateTime.Today;

            AnsiConsole.MarkupLine($"\n[yellow]Date range:[/] [cyan]{startDate:yyyy-MM-dd}[/] to [cyan]{endDate:yyyy-MM-dd}[/]\n");

            var config = new BacktestBuilder()
                .ForTicker(ticker)
                .WithDateRange(startDate, endDate)
                .WithInitialCapital(initialCapitalInput)
                .WithCommission(0.001m, 1.0m)
                .Build();

            BacktestResult? result = null;

            await AnsiConsole.Status()
                .StartAsync("Running backtest...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("cyan"));

                    var progress = new Progress<(int current, int total, int trades)>(p =>
                    {
                        ctx.Status($"Processing: {p.current}/{p.total} bars, {p.trades} trades");
                    });

                    result = await backtestEngine.RunBacktestAsync(selectedStrategy, config, progress);

                    ctx.Status("[green]Backtest complete[/]");
                    await Task.Delay(300);
                });

            if (result == null)
            {
                AnsiConsole.MarkupLine("[red]✗ Backtest failed[/]");
                return;
            }

            AnsiConsole.MarkupLine("[green]✓[/] Backtest complete\n");

            presenter.DisplayResults(result);

            if (AnsiConsole.Profile.Capabilities.Interactive)
            {
                var exportChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("\n[yellow]Export results?[/]")
                        .PageSize(10)
                        .AddChoices("Skip", "JSON"));

                if (exportChoice == "JSON")
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var filename = $"backtest_{ticker}_{selectedStrategy.Name.Replace(" ", "_")}_{timestamp}.json";

                    await exportService.ExportBacktestResultAsync(result, filename);
                    AnsiConsole.MarkupLine($"\n[green]✓[/] Exported to {filename}");
                }
            }
        }
        catch (Exception ex)
        {
            var errorPanel = new Panel(
                $"[red]{ex.Message.EscapeMarkup()}[/]\n\n" +
                (ex.InnerException != null ? $"[dim]Details: {ex.InnerException.Message.EscapeMarkup()}[/]" : ""))
            {
                Header = new PanelHeader("[red bold]Error[/]"),
                Border = BoxBorder.Rounded,
                BorderStyle = Style.Parse("red")
            };

            AnsiConsole.Write(errorPanel);
            Environment.Exit(1);
        }
    }
}
