using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using TradingStrat.Data;
using TradingStrat.Models;
using TradingStrat.Services;
using TradingStrat.Services.LiveAnalysis;
using TradingStrat.Services.Strategies.MachineLearning;

namespace TradingStrat;

public static class ProgramAnalyze
{
    public static async Task RunAsync()
    {
        AnsiConsole.Write(
            new FigletText("Live Analysis")
                .LeftJustified()
                .Color(Color.Green));

        AnsiConsole.MarkupLine("[grey]ML-Powered Position Analysis[/]\n");

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
            var yahooService = new YahooFinanceService();
            var analysisEngine = new LiveAnalysisEngine(repository, yahooService);
            var presenter = new AnalysisPresenter();

            // Get ticker (hardcoded for now, matching ProgramBacktest.cs)
            const string ticker = "CON3.L";

            // Check if we have data
            var dataCount = await context.HistoricalPrices.CountAsync(p => p.Ticker == ticker);
            if (dataCount < 30)
            {
                AnsiConsole.MarkupLine($"[red]✗ ERROR:[/] Insufficient data for {ticker}");
                AnsiConsole.MarkupLine($"[yellow]Required: 30+ bars, Available: {dataCount}[/]");
                AnsiConsole.MarkupLine("[yellow]Please run the data fetcher first[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[green]✓[/] Found {dataCount:N0} historical bars for {ticker}\n");

            // Select threshold strategy
            var thresholdChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Select prediction thresholds:[/]")
                    .PageSize(10)
                    .AddChoices(
                        "1% Thresholds (±1%)",
                        "0.5% Thresholds (±0.5%)",
                        "Custom Thresholds"));

            PredictionThresholds thresholds = thresholdChoice switch
            {
                "1% Thresholds (±1%)" => new PredictionThresholds(0.01m, -0.01m),
                "0.5% Thresholds (±0.5%)" => new PredictionThresholds(0.005m, -0.005m),
                "Custom Thresholds" => GetCustomThresholds(),
                _ => new PredictionThresholds()
            };

            AnsiConsole.WriteLine();

            // Run analysis with progress
            LiveAnalysisResult? result = null;

            await AnsiConsole.Status()
                .StartAsync("Analyzing current position...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("cyan"));

                    var progress = new Progress<string>(status =>
                    {
                        ctx.Status(status);
                    });

                    result = await analysisEngine.AnalyzeCurrentPositionAsync(
                        ticker, thresholds, progress);

                    ctx.Status("[green]Analysis complete[/]");
                    await Task.Delay(300);
                });

            if (result == null)
            {
                AnsiConsole.MarkupLine("[red]✗ Analysis failed[/]");
                return;
            }

            AnsiConsole.MarkupLine("[green]✓[/] Analysis complete\n");

            // Display results
            presenter.DisplayAnalysis(result);

            // Optional: Export results
            if (AnsiConsole.Profile.Capabilities.Interactive)
            {
                var exportChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("\n[yellow]Export analysis?[/]")
                        .PageSize(10)
                        .AddChoices("Skip", "JSON"));

                if (exportChoice == "JSON")
                {
                    var exportService = new ExportService();
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var filename = $"live_analysis_{ticker}_{timestamp}.json";

                    await exportService.ExportLiveAnalysisAsync(result, filename);
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
        }
    }

    private static PredictionThresholds GetCustomThresholds()
    {
        var buyThreshold = AnsiConsole.Ask<decimal>(
            "[yellow]Buy threshold (%):[/]", 1.0m) / 100m;

        var sellThreshold = AnsiConsole.Ask<decimal>(
            "[yellow]Sell threshold (%):[/]", -1.0m) / 100m;

        return new PredictionThresholds(buyThreshold, sellThreshold);
    }
}
