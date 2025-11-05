// <copyright file="PerformanceExportCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Cli;
using TradingBot.Core.Interfaces;

namespace TradingBot.Cli.Commands.Performance;

/// <summary>
/// Command to export performance metrics to file.
/// </summary>
public sealed class PerformanceExportCommand : AsyncCommand<PerformanceExportCommand.Settings>
{
    private readonly IPortfolioManager _portfolioManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceExportCommand"/> class.
    /// </summary>
    /// <param name="portfolioManager">Portfolio manager.</param>
    public PerformanceExportCommand(IPortfolioManager portfolioManager)
    {
        _portfolioManager = portfolioManager ?? throw new ArgumentNullException(nameof(portfolioManager));
    }

    /// <inheritdoc/>
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var metrics = await _portfolioManager.GetPerformanceMetricsAsync();
        var account = await _portfolioManager.GetAccountAsync();
        var trades = await _portfolioManager.GetTradeHistoryAsync();

        var exportData = new
        {
            ExportDate = DateTime.UtcNow,
            Account = new
            {
                account.AccountId,
                account.Equity,
                account.Cash,
                account.RealizedPnL,
                account.UnrealizedPnL,
            },
            Performance = new
            {
                metrics.TotalReturn,
                metrics.SharpeRatio,
                metrics.SortinoRatio,
                metrics.CalmarRatio,
                metrics.MaxDrawdown,
                metrics.ProfitFactor,
                metrics.TotalTrades,
                metrics.WinningTrades,
                metrics.LosingTrades,
                metrics.WinRate,
                metrics.AverageWin,
                metrics.AverageLoss,
            },
            Trades = trades.Select(t => new
            {
                t.Symbol,
                Side = t.Side.ToString(),
                t.Quantity,
                t.EntryPrice,
                t.ExitPrice,
                t.EntryTime,
                t.ExitTime,
                t.RealizedPnL,
                t.Commission,
                t.StrategyName,
            }),
        };

        var outputPath = settings.OutputPath ?? $"performance-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";

        try
        {
            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true,
            });

            await File.WriteAllTextAsync(outputPath, json);

            AnsiConsole.MarkupLine($"[green]✓[/] Performance data exported to [cyan]{outputPath}[/]");
            AnsiConsole.WriteLine();

            var fileInfo = new FileInfo(outputPath);
            AnsiConsole.MarkupLine($"[dim]File size: {fileInfo.Length:N0} bytes[/]");
            AnsiConsole.MarkupLine($"[dim]Total trades: {trades.Count}[/]");

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to export: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Settings for the performance export command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets or sets the output file path.
        /// </summary>
        [CommandOption("--output|-o")]
        [Description("Output file path (default: performance-export-{timestamp}.json)")]
        public string? OutputPath { get; set; }
    }
}
