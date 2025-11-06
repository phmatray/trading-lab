// <copyright file="PerformanceExportCommand.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.ComponentModel;
using System.Globalization;
using System.Text;
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
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
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

        // Determine format and default file extension
        var format = settings.Format?.ToLowerInvariant() ?? "json";
        var extension = format == "csv" ? ".csv" : ".json";
        var outputPath = settings.OutputPath ?? $"performance-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}{extension}";

        try
        {
            string content;

            if (format == "csv")
            {
                content = ExportToCsv(account, metrics, trades);
            }
            else
            {
                var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
                {
                    WriteIndented = true,
                });
                content = json;
            }

            await File.WriteAllTextAsync(outputPath, content);

            AnsiConsole.MarkupLine($"[green]✓[/] Performance data exported to [cyan]{outputPath}[/] ([yellow]{format.ToUpperInvariant()}[/])");
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

    private static string ExportToCsv(
        Core.Models.Portfolio.Account account,
        Core.Models.Portfolio.PerformanceMetrics metrics,
        IReadOnlyList<Core.Models.Trading.Trade> trades)
    {
        var csv = new StringBuilder();

        // Account summary
        csv.AppendLine("ACCOUNT SUMMARY");
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Account ID,{account.AccountId}");
        csv.AppendLine($"Equity,{account.Equity:F2}");
        csv.AppendLine($"Cash,{account.Cash:F2}");
        csv.AppendLine($"Realized P&L,{account.RealizedPnL:F2}");
        csv.AppendLine($"Unrealized P&L,{account.UnrealizedPnL:F2}");
        csv.AppendLine();

        // Performance metrics
        csv.AppendLine("PERFORMANCE METRICS");
        csv.AppendLine("Metric,Value");
        csv.AppendLine($"Total Return,{metrics.TotalReturn:P2}");
        csv.AppendLine($"Sharpe Ratio,{metrics.SharpeRatio:F2}");
        csv.AppendLine($"Sortino Ratio,{metrics.SortinoRatio:F2}");
        csv.AppendLine($"Calmar Ratio,{metrics.CalmarRatio:F2}");
        csv.AppendLine($"Max Drawdown,{metrics.MaxDrawdown:P2}");
        csv.AppendLine($"Profit Factor,{metrics.ProfitFactor:F2}");
        csv.AppendLine($"Total Trades,{metrics.TotalTrades}");
        csv.AppendLine($"Winning Trades,{metrics.WinningTrades}");
        csv.AppendLine($"Losing Trades,{metrics.LosingTrades}");
        csv.AppendLine($"Win Rate,{metrics.WinRate:P2}");
        csv.AppendLine($"Average Win,{metrics.AverageWin:F2}");
        csv.AppendLine($"Average Loss,{metrics.AverageLoss:F2}");
        csv.AppendLine();

        // Trades
        csv.AppendLine("TRADE HISTORY");
        csv.AppendLine("Symbol,Side,Quantity,Entry Price,Exit Price,Entry Time,Exit Time,P&L,Commission,Strategy");

        foreach (var trade in trades)
        {
            csv.AppendLine($"{trade.Symbol}," +
                          $"{trade.Side}," +
                          $"{trade.Quantity}," +
                          $"{trade.EntryPrice:F2}," +
                          $"{trade.ExitPrice:F2}," +
                          $"{trade.EntryTime:yyyy-MM-dd HH:mm:ss}," +
                          $"{trade.ExitTime:yyyy-MM-dd HH:mm:ss}," +
                          $"{trade.RealizedPnL:F2}," +
                          $"{trade.Commission:F2}," +
                          $"{trade.StrategyName}");
        }

        return csv.ToString();
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
        [Description("Output file path (default: performance-export-{timestamp}.{format})")]
        public string? OutputPath { get; set; }

        /// <summary>
        /// Gets or sets the export format (json or csv).
        /// </summary>
        [CommandOption("--format|-f")]
        [Description("Export format: json or csv (default: json)")]
        public string? Format { get; set; }
    }
}
