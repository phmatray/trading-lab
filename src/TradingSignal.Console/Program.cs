using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using TradingSignal.ConsoleApp.Commands;
using TradingSignal.ConsoleApp.Configuration;
using TradingSignal.Core.Abstractions;
using TradingSignal.Data;
using TradingSignal.Data.Binance;
using TradingSignal.Data.Caching;
using TradingSignal.Evaluation.Stores;
using TradingSignal.Indicators;
using TradingSignal.Llm;
using TradingSignal.Llm.Caching;

namespace TradingSignal.ConsoleApp;

internal static class Program
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Top-level process boundary: any uncaught exception must be logged and returned as exit code, not propagated.")]
    public static async Task<int> Main(string[] args)
    {
        string command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";
        if (command is "help" or "-h" or "--help" or "/?")
        {
            PrintHelp();
            return 0;
        }

        using IHost host = BuildHost();
        using CancellationTokenSource cts = new();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        try
        {
            return command switch
            {
                "ingest" => await host.Services.GetRequiredService<IngestCommand>().ExecuteAsync(cts.Token),
                "run" => await host.Services.GetRequiredService<RunCommand>().ExecuteAsync(cts.Token),
                "report" => await host.Services.GetRequiredService<ReportCommand>().ExecuteAsync(cts.Token),
                _ => PrintHelpAndExit($"unknown command: {command}"),
            };
        }
        catch (OperationCanceledException)
        {
            await Console.Error.WriteLineAsync("cancelled").ConfigureAwait(false);
            return 130;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Unhandled exception");
            return 1;
        }
    }

    private static IHost BuildHost()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables(prefix: "TSIG_")
            .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        IHostBuilder builder = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((ctx, services) =>
            {
                AppConfig appConfig = new();
                configuration.Bind(appConfig);
                services.AddSingleton(appConfig);

                services.AddSingleton<LmStudioOptions>(sp =>
                {
                    LmStudioConfig src = sp.GetRequiredService<AppConfig>().LmStudio;
                    return new LmStudioOptions
                    {
                        Endpoint = src.Endpoint,
                        ModelId = src.ModelId,
                        TimeoutSeconds = src.TimeoutSeconds,
                        MaxFewShot = src.MaxFewShot,
                        MaxOutputTokens = src.MaxOutputTokens,
                    };
                });

                services.AddSingleton<ICandleCache>(sp =>
                    new CsvCandleCache(sp.GetRequiredService<AppConfig>().Output.DataCacheDir));
                services.AddSingleton<IKlineFetcher>(_ => new BinanceKlineFetcher());
                services.AddSingleton<IMarketDataSource>(sp => new BinanceMarketDataSource(
                    sp.GetRequiredService<IKlineFetcher>(),
                    sp.GetRequiredService<ICandleCache>(),
                    sp.GetService<ILogger<BinanceMarketDataSource>>()));

                services.AddSingleton<ILlmResponseCache>(sp =>
                    new SqliteLlmResponseCache(sp.GetRequiredService<AppConfig>().Output.LlmCachePath));
                services.AddSingleton(sp => LmStudioChatClientFactory.Create(sp.GetRequiredService<LmStudioOptions>()));
                services.AddSingleton<ISignalGenerator>(sp => new LlmSignalGenerator(
                    sp.GetRequiredService<Microsoft.Extensions.AI.IChatClient>(),
                    sp.GetRequiredService<LmStudioOptions>(),
                    sp.GetRequiredService<ILlmResponseCache>(),
                    sp.GetService<ILogger<LlmSignalGenerator>>()));

                services.AddSingleton<IFeatureEngine>(sp => new FeatureEngine(sp.GetRequiredService<AppConfig>().Market.Symbol));
                services.AddSingleton<IPredictionStore>(sp =>
                    new SqlitePredictionStore(sp.GetRequiredService<AppConfig>().Output.DbPath));

                services.AddTransient<IngestCommand>();
                services.AddTransient<RunCommand>();
                services.AddTransient<ReportCommand>();
            });

        return builder.Build();
    }

    private static void PrintHelp()
    {
        Console.WriteLine("TradingSignal.Console — crypto signal walk-forward PoC");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  TradingSignal.Console ingest     fetch & cache market data");
        Console.WriteLine("  TradingSignal.Console run        run full walk-forward backtest");
        Console.WriteLine("  TradingSignal.Console report     print the latest run's metrics table");
        Console.WriteLine();
        Console.WriteLine("Configuration: appsettings.json next to the binary.");
        Console.WriteLine("LM Studio must be running locally for `run` (default http://localhost:1234/v1).");
    }

    private static int PrintHelpAndExit(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine();
        PrintHelp();
        return 2;
    }
}
