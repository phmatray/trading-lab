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

                // CLI-specific services
                services.AddTransient<Dashboard.DashboardRenderer>();

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

                        strategyBranch.AddCommand<Commands.Strategy.StrategyStartCommand>("start")
                            .WithDescription("Start the strategy engine")
                            .WithExample(["strategy", "start"])
                            .WithExample(["strategy", "start", "--interval", "30"]);

                        strategyBranch.AddCommand<Commands.Strategy.StrategyStopCommand>("stop")
                            .WithDescription("Stop the strategy engine")
                            .WithExample(["strategy", "stop"]);

                        strategyBranch.AddCommand<Commands.Strategy.StrategyStatusCommand>("status")
                            .WithDescription("Show strategy engine status")
                            .WithExample(["strategy", "status"]);
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

                    // Risk commands
                    config.AddBranch("risk", riskBranch =>
                    {
                        riskBranch.SetDescription("Manage risk settings");

                        riskBranch.AddCommand<Commands.Risk.RiskShowCommand>("show")
                            .WithDescription("Display current risk settings")
                            .WithExample(["risk", "show"]);

                        riskBranch.AddCommand<Commands.Risk.RiskSetLeverageCommand>("set-leverage")
                            .WithDescription("Set account leverage")
                            .WithExample(["risk", "set-leverage", "2.0"]);

                        riskBranch.AddCommand<Commands.Risk.RiskSetStopLossCommand>("set-stop-loss")
                            .WithDescription("Set default stop-loss percentage")
                            .WithExample(["risk", "set-stop-loss", "2.5"]);

                        riskBranch.AddCommand<Commands.Risk.RiskSetTakeProfitCommand>("set-take-profit")
                            .WithDescription("Set default take-profit percentage")
                            .WithExample(["risk", "set-take-profit", "5.0"]);

                        riskBranch.AddCommand<Commands.Risk.RiskSetDailyLossCommand>("set-daily-loss")
                            .WithDescription("Set maximum daily loss limit")
                            .WithExample(["risk", "set-daily-loss", "1000"]);

                        riskBranch.AddCommand<Commands.Risk.RiskSetMaxDrawdownCommand>("set-max-drawdown")
                            .WithDescription("Set maximum drawdown percentage")
                            .WithExample(["risk", "set-max-drawdown", "15.0"]);

                        riskBranch.AddCommand<Commands.Risk.RiskResetCommand>("reset")
                            .WithDescription("Reset risk settings to defaults")
                            .WithExample(["risk", "reset"]);
                    });

                    // Performance commands
                    config.AddBranch("performance", performanceBranch =>
                    {
                        performanceBranch.SetDescription("View performance metrics");

                        performanceBranch.AddCommand<Commands.Performance.PerformanceShowCommand>("show")
                            .WithDescription("Display performance metrics and statistics")
                            .WithExample(["performance", "show"]);

                        performanceBranch.AddCommand<Commands.Performance.PerformanceExportCommand>("export")
                            .WithDescription("Export performance data to file")
                            .WithExample(["performance", "export"])
                            .WithExample(["performance", "export", "--output", "perf-report.json"])
                            .WithExample(["performance", "export", "--format", "csv"])
                            .WithExample(["performance", "export", "--format", "csv", "--output", "report.csv"]);
                    });

                    // Backtest commands
                    config.AddBranch("backtest", backtestBranch =>
                    {
                        backtestBranch.SetDescription("Run and analyze backtests");

                        backtestBranch.AddCommand<Commands.Backtest.BacktestRunCommand>("run")
                            .WithDescription("Run a strategy backtest")
                            .WithExample(["backtest", "run", "momentum"])
                            .WithExample(["backtest", "run", "momentum", "--symbol", "SPY", "--start-date", "2024-01-01"]);

                        backtestBranch.AddCommand<Commands.Backtest.BacktestReportCommand>("report")
                            .WithDescription("Display backtest report")
                            .WithExample(["backtest", "report", "latest"])
                            .WithExample(["backtest", "report", "abc123"]);
                    });

                    // Dashboard command
                    config.AddCommand<DashboardCommand>("dashboard")
                        .WithDescription("Display trading dashboard with real-time updates")
                        .WithExample(["dashboard"])
                        .WithExample(["dashboard", "--live"])
                        .WithExample(["dashboard", "--refresh", "5"])
                        .WithExample(["dashboard", "--live=false"]);
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
