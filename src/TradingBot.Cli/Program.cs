// <copyright file="Program.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Cli.Commands;
using TradingBot.Cli.Infrastructure;
using TradingBot.Infrastructure.DependencyInjection;

namespace TradingBot.Cli;

/// <summary>
/// Main program entry point for the TradingBot CLI.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    public static int Main(string[] args)
    {
        try
        {
            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables(prefix: "TRADINGBOT_")
                .Build();

            // Configure Serilog
            Log.Logger = ServiceCollectionExtensions.ConfigureSerilog(configuration);

            try
            {
                Log.Information("TradingBot CLI starting up...");

                // Create DI container
                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(configuration);
                services.AddTradingBotServices(configuration);

                // Create and configure command app
                var registrar = new TypeRegistrar(services);
                var app = new CommandApp(registrar);

                app.Configure(config =>
                {
                    config.SetApplicationName("tradingbot");
                    config.SetApplicationVersion("1.0.0");

                    config.ValidateExamples();

                    // Global exception handler
                    config.PropagateExceptions();

                    // Version command
                    config.AddCommand<VersionCommand>("version")
                        .WithDescription("Display version information")
                        .WithExample(["version"]);

                    // Config commands
                    config.AddBranch("config", configBranch =>
                    {
                        configBranch.SetDescription("Manage application configuration");

                        configBranch.AddCommand<Commands.Config.ConfigShowCommand>("show")
                            .WithDescription("Display current configuration")
                            .WithExample(["config", "show"]);

                        configBranch.AddCommand<Commands.Config.ConfigSetCommand>("set")
                            .WithDescription("Set a configuration value")
                            .WithExample(["config", "set", "DefaultSymbols", "SPY,QQQ,AAPL"]);

                        configBranch.AddCommand<Commands.Config.ConfigSetApiKeyCommand>("set-api-key")
                            .WithDescription("Set an API key (interactive, encrypted)")
                            .WithExample(["config", "set-api-key", "yahoo"]);
                    });

                    // Strategy commands
                    config.AddBranch("strategy", strategyBranch =>
                    {
                        strategyBranch.SetDescription("Manage trading strategies");

                        strategyBranch.AddCommand<Commands.Strategy.StrategyListCommand>("list")
                            .WithDescription("List all registered strategies")
                            .WithExample(["strategy", "list"]);

                        strategyBranch.AddCommand<Commands.Strategy.StrategyEnableCommand>("enable")
                            .WithDescription("Enable a strategy")
                            .WithExample(["strategy", "enable", "momentum-spy"]);

                        strategyBranch.AddCommand<Commands.Strategy.StrategyDisableCommand>("disable")
                            .WithDescription("Disable a strategy")
                            .WithExample(["strategy", "disable", "momentum-spy"]);
                    });

                    // Portfolio commands
                    config.AddBranch("portfolio", portfolioBranch =>
                    {
                        portfolioBranch.SetDescription("View and manage portfolio");

                        portfolioBranch.AddCommand<Commands.Portfolio.PortfolioShowCommand>("show")
                            .WithDescription("Display current portfolio positions")
                            .WithExample(["portfolio", "show"]);

                        portfolioBranch.AddCommand<Commands.Portfolio.PortfolioHistoryCommand>("history")
                            .WithDescription("Display trade history")
                            .WithExample(["portfolio", "history"])
                            .WithExample(["portfolio", "history", "--symbol", "SPY"])
                            .WithExample(["portfolio", "history", "--start-date", "2025-01-01", "--limit", "50"]);

                        portfolioBranch.AddCommand<Commands.Portfolio.PortfolioCloseCommand>("close")
                            .WithDescription("Close one or all positions")
                            .WithExample(["portfolio", "close", "--symbol", "SPY"])
                            .WithExample(["portfolio", "close", "--all"]);
                    });

                    // Placeholder for other commands
                    // config.AddBranch("risk", risk => { ... });
                    // config.AddBranch("portfolio", portfolio => { ... });
                    // config.AddBranch("performance", performance => { ... });
                    // config.AddBranch("backtest", backtest => { ... });
                    // config.AddCommand<DashboardCommand>("dashboard");
                });

                return app.Run(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
                AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        catch (Exception ex)
        {
            // Fallback error handling if Serilog setup fails
            AnsiConsole.MarkupLine($"[red]Fatal error during startup:[/] {ex.Message}");
            return 1;
        }
    }
}
