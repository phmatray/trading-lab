using Microsoft.Extensions.Options;
using Spectre.Console;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Presentation.Console.Presenters;

namespace TradingStrat.Presentation.Console;

public class ProgramMenu
{
    private readonly IDataFetchingUseCase _dataFetchingUseCase;
    private readonly IBacktestUseCase _backtestUseCase;
    private readonly ILiveAnalysisUseCase _liveAnalysisUseCase;
    private readonly IParameterOptimizationUseCase _parameterOptimizationUseCase;
    private readonly IExportPort _exportPort;
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly TradingConfiguration _config;

    public ProgramMenu(
        IDataFetchingUseCase dataFetchingUseCase,
        IBacktestUseCase backtestUseCase,
        ILiveAnalysisUseCase liveAnalysisUseCase,
        IParameterOptimizationUseCase parameterOptimizationUseCase,
        IExportPort exportPort,
        IHistoricalDataPort historicalDataPort,
        IOptions<TradingConfiguration> config)
    {
        _dataFetchingUseCase = dataFetchingUseCase;
        _backtestUseCase = backtestUseCase;
        _liveAnalysisUseCase = liveAnalysisUseCase;
        _parameterOptimizationUseCase = parameterOptimizationUseCase;
        _exportPort = exportPort;
        _historicalDataPort = historicalDataPort;
        _config = config.Value;
    }

    public async Task RunAsync()
    {
        AnsiConsole.Write(
            new FigletText("Trading Strategy")
                .LeftJustified()
                .Color(Color.Cyan1));

        AnsiConsole.MarkupLine("[grey]Historical Data & Backtesting System[/]\n");

        while (true)
        {
            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Main Menu:[/]")
                    .PageSize(10)
                    .AddChoices(
                        "1. Fetch/Update Historical Data",
                        "2. Run Backtest",
                        "3. A/B Parameter Optimization",
                        "4. Analyze Current Position",
                        "5. Exit"));

            AnsiConsole.WriteLine();

            switch (choice)
            {
                case "1. Fetch/Update Historical Data":
                    await RunDataFetcherAsync();
                    break;
                case "2. Run Backtest":
                    await RunBacktestAsync();
                    break;
                case "3. A/B Parameter Optimization":
                    await RunParameterOptimizationAsync();
                    break;
                case "4. Analyze Current Position":
                    await RunAnalysisAsync();
                    break;
                case "5. Exit":
                    AnsiConsole.MarkupLine("[grey]Goodbye![/]");
                    return;
            }

            if (choice != "5. Exit")
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[dim]Press any key to return to menu...[/]");
                System.Console.ReadKey(true);
                AnsiConsole.Clear();

                AnsiConsole.Write(
                    new FigletText("Trading Strategy")
                        .LeftJustified()
                        .Color(Color.Cyan1));

                AnsiConsole.MarkupLine("[grey]Historical Data & Backtesting System[/]\n");
            }
        }
    }

    private async Task RunDataFetcherAsync()
    {
        try
        {
            string ticker = _config.DefaultTicker;
            string isin = _config.DefaultIsin;

            var command = new FetchDataCommand(ticker, isin);

            DataSummaryResult? result = null;

            await AnsiConsole.Status()
                .StartAsync("Fetching data...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("cyan"));

                    var progress = new Progress<string>(status =>
                    {
                        ctx.Status(status);
                    });

                    result = await _dataFetchingUseCase.ExecuteAsync(command, progress);
                });

            if (result != null)
            {
                DataSummaryPresenter.Display(result);

                await HandleExportAsync(result.Ticker);
            }
        }
        catch (Exception ex)
        {
            DisplayError(ex);
        }
    }

    private async Task RunBacktestAsync()
    {
        try
        {
            string ticker = _config.DefaultTicker;

            string strategy = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Select trading strategy:[/]")
                    .PageSize(10)
                    .AddChoices(
                        "MA Crossover (20/50)",
                        "MA Crossover (10/30)",
                        "RSI (14, 30/70)",
                        "MACD (12/26/9)",
                        "ML FastTree (1% thresholds)",
                        "ML FastTree (0.5% thresholds)"));

            (string strategyType, Dictionary<string, object>? strategyParams) = ParseStrategyChoice(strategy);

            decimal initialCapital = AnsiConsole.Ask(
                "[yellow]Initial capital:[/]",
                _config.Backtest.InitialCapital);

            AnsiConsole.WriteLine();

            var command = new BacktestCommand(
                ticker,
                strategyType,
                strategyParams,
                initialCapital,
                _config.Backtest.CommissionPercentage,
                _config.Backtest.MinimumCommission);

            BacktestResult? result = null;

            await AnsiConsole.Status()
                .StartAsync("Running backtest...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("cyan"));

                    var progress = new Progress<BacktestProgress>(p =>
                    {
                        ctx.Status($"Processing: {p.Current}/{p.Total} bars, {p.Trades} trades");
                    });

                    result = await _backtestUseCase.ExecuteAsync(command, progress);

                    ctx.Status("[green]Backtest complete[/]");
                    await Task.Delay(300);
                });

            if (result != null)
            {
                AnsiConsole.MarkupLine("[green]✓[/] Backtest complete\n");
                BacktestPresenter.DisplayResults(result);

                await HandleBacktestExportAsync(result, ticker, strategyType);
            }
        }
        catch (Exception ex)
        {
            DisplayError(ex);
        }
    }

    private async Task RunAnalysisAsync()
    {
        try
        {
            string ticker = _config.DefaultTicker;

            string thresholdChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Select prediction thresholds:[/]")
                    .PageSize(10)
                    .AddChoices(
                        "1% Thresholds (±1%)",
                        "0.5% Thresholds (±0.5%)",
                        "Custom Thresholds"));

            PredictionThresholds thresholds = ParseThresholdChoice(thresholdChoice);

            AnsiConsole.WriteLine();

            var command = new AnalysisCommand(ticker, thresholds);

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

                    result = await _liveAnalysisUseCase.ExecuteAsync(command, progress);

                    ctx.Status("[green]Analysis complete[/]");
                    await Task.Delay(300);
                });

            if (result != null)
            {
                AnsiConsole.MarkupLine("[green]✓[/] Analysis complete\n");
                AnalysisPresenter.DisplayAnalysis(result);

                await HandleAnalysisExportAsync(result, ticker);
            }
        }
        catch (Exception ex)
        {
            DisplayError(ex);
        }
    }

    private async Task RunParameterOptimizationAsync()
    {
        try
        {
            string ticker = _config.DefaultTicker;

            // Select strategy type
            string strategyType = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Select strategy type:[/]")
                    .PageSize(10)
                    .AddChoices(
                        "Moving Average Crossover",
                        "RSI",
                        "MACD",
                        "ML FastTree"));

            string strategyKey = strategyType switch
            {
                "Moving Average Crossover" => "ma",
                "RSI" => "rsi",
                "MACD" => "macd",
                "ML FastTree" => "ml",
                _ => "ma"
            };

            // Get predefined variants
            List<(StrategyVariant VariantA, StrategyVariant VariantB)> variantPairs =
                ParameterGenerator.GetPredefinedVariants(strategyKey);

            // Let user select which A/B test to run
            List<string> choices = variantPairs
                .Select((pair, idx) => $"{idx + 1}. {pair.VariantA.Description} vs {pair.VariantB.Description}")
                .ToList();

            string selectedPair = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Select parameter variants to compare:[/]")
                    .PageSize(10)
                    .AddChoices(choices));

            int selectedIndex = int.Parse(selectedPair.Split('.')[0]) - 1;
            (StrategyVariant variantA, StrategyVariant variantB) = variantPairs[selectedIndex];

            decimal initialCapital = AnsiConsole.Ask(
                "[yellow]Initial capital:[/]",
                _config.Backtest.InitialCapital);

            AnsiConsole.WriteLine();

            ParameterOptimizationCommand command = new ParameterOptimizationCommand(
                ticker,
                variantA,
                variantB,
                initialCapital,
                _config.Backtest.CommissionPercentage,
                _config.Backtest.MinimumCommission);

            ParameterOptimizationResult? result = null;

            await AnsiConsole.Status()
                .StartAsync("Running A/B optimization...", async ctx =>
                {
                    ctx.Spinner(Spinner.Known.Dots);
                    ctx.SpinnerStyle(Style.Parse("cyan"));

                    Progress<OptimizationProgress> progress = new Progress<OptimizationProgress>(p =>
                    {
                        ctx.Status($"{p.CurrentVariant}: {p.CurrentBar}/{p.TotalBars} bars, {p.Trades} trades");
                    });

                    result = await _parameterOptimizationUseCase.ExecuteAsync(command, progress);

                    ctx.Status("[green]Optimization complete[/]");
                    await Task.Delay(300);
                });

            if (result != null)
            {
                AnsiConsole.MarkupLine("[green]✓[/] A/B optimization complete\n");
                ParameterOptimizationPresenter.DisplayResults(result);
            }
        }
        catch (Exception ex)
        {
            DisplayError(ex);
        }
    }

    private (string strategyType, Dictionary<string, object>? parameters) ParseStrategyChoice(string choice)
    {
        return choice switch
        {
            "MA Crossover (20/50)" => ("ma", new Dictionary<string, object>
            {
                ["FastPeriod"] = 20,
                ["SlowPeriod"] = 50
            }),
            "MA Crossover (10/30)" => ("ma", new Dictionary<string, object>
            {
                ["FastPeriod"] = 10,
                ["SlowPeriod"] = 30
            }),
            "RSI (14, 30/70)" => ("rsi", new Dictionary<string, object>
            {
                ["Period"] = 14,
                ["OversoldThreshold"] = 30,
                ["OverboughtThreshold"] = 70
            }),
            "MACD (12/26/9)" => ("macd", new Dictionary<string, object>
            {
                ["FastPeriod"] = 12,
                ["SlowPeriod"] = 26,
                ["SignalPeriod"] = 9
            }),
            "ML FastTree (1% thresholds)" => ("ml", new Dictionary<string, object>
            {
                ["BuyThreshold"] = 0.01m,
                ["SellThreshold"] = -0.01m,
                ["MinTrainingBars"] = 100
            }),
            "ML FastTree (0.5% thresholds)" => ("ml", new Dictionary<string, object>
            {
                ["BuyThreshold"] = 0.005m,
                ["SellThreshold"] = -0.005m,
                ["MinTrainingBars"] = 100
            }),
            _ => ("ma", new Dictionary<string, object>
            {
                ["FastPeriod"] = 20,
                ["SlowPeriod"] = 50
            })
        };
    }

    private PredictionThresholds ParseThresholdChoice(string choice)
    {
        return choice switch
        {
            "1% Thresholds (±1%)" => new PredictionThresholds(0.01m, -0.01m),
            "0.5% Thresholds (±0.5%)" => new PredictionThresholds(0.005m, -0.005m),
            "Custom Thresholds" => GetCustomThresholds(),
            _ => new PredictionThresholds()
        };
    }

    private PredictionThresholds GetCustomThresholds()
    {
        decimal buyThreshold = AnsiConsole.Ask(
            "[yellow]Buy threshold (%):[/]", 1.0m) / 100m;

        decimal sellThreshold = AnsiConsole.Ask(
            "[yellow]Sell threshold (%):[/]", -1.0m) / 100m;

        return new PredictionThresholds(buyThreshold, sellThreshold);
    }

    private async Task HandleExportAsync(string ticker)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            return;
        }

        string exportChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[yellow]Export data?[/]")
                .PageSize(10)
                .AddChoices("Skip", "CSV", "JSON", "Both (CSV + JSON)"));

        if (exportChoice == "Skip")
        {
            return;
        }

        List<HistoricalPrice> data = await _historicalDataPort.GetHistoricalDataAsync(ticker);
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        await AnsiConsole.Status()
            .StartAsync("Exporting data...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Star);
                ctx.SpinnerStyle(Style.Parse("yellow"));

                switch (exportChoice)
                {
                    case "CSV":
                        await _exportPort.ExportToCsvAsync(data, $"trading_{ticker}_{timestamp}.csv");
                        break;

                    case "JSON":
                        await _exportPort.ExportToJsonAsync(data, $"trading_{ticker}_{timestamp}.json");
                        break;

                    case "Both (CSV + JSON)":
                        await _exportPort.ExportToCsvAsync(data, $"trading_{ticker}_{timestamp}.csv");
                        await _exportPort.ExportToJsonAsync(data, $"trading_{ticker}_{timestamp}.json");
                        break;
                }

                await Task.Delay(300);
            });

        AnsiConsole.MarkupLine("[green]✓[/] Export completed successfully");
    }

    private async Task HandleBacktestExportAsync(BacktestResult result, string ticker, string strategyType)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            return;
        }

        string exportChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("\n[yellow]Export results?[/]")
                .PageSize(10)
                .AddChoices("Skip", "JSON"));

        if (exportChoice == "JSON")
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"backtest_{ticker}_{strategyType.Replace(" ", "_")}_{timestamp}.json";

            await _exportPort.ExportToJsonAsync(result, filename);
            AnsiConsole.MarkupLine($"\n[green]✓[/] Exported to {filename}");
        }
    }

    private async Task HandleAnalysisExportAsync(LiveAnalysisResult result, string ticker)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            return;
        }

        string exportChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("\n[yellow]Export analysis?[/]")
                .PageSize(10)
                .AddChoices("Skip", "JSON"));

        if (exportChoice == "JSON")
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"live_analysis_{ticker}_{timestamp}.json";

            await _exportPort.ExportToJsonAsync(result, filename);
            AnsiConsole.MarkupLine($"\n[green]✓[/] Exported to {filename}");
        }
    }

    private void DisplayError(Exception ex)
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
