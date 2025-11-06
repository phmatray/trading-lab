// <copyright file="BacktestReportGenerator.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using System.Globalization;
using System.Text;
using System.Text.Json;
using TradingBot.Core.Models.Backtest;

namespace TradingBot.Analytics;

/// <summary>
/// Generates backtest reports in multiple formats (Console, JSON, CSV, HTML).
/// </summary>
public sealed class BacktestReportGenerator
{
    /// <summary>
    /// Generates a formatted console report.
    /// </summary>
    /// <param name="result">The backtest result.</param>
    /// <returns>Formatted console report string.</returns>
    public string GenerateConsoleReport(BacktestResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("                    BACKTEST REPORT");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();

        // Summary section
        sb.AppendLine("SUMMARY");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine($"Backtest ID:       {result.BacktestId}");
        sb.AppendLine($"Strategy:          {result.StrategyName}");
        sb.AppendLine($"Symbol:            {result.Symbol}");
        sb.AppendLine($"Period:            {result.StartDate:yyyy-MM-dd} to {result.EndDate:yyyy-MM-dd}");
        sb.AppendLine($"Duration:          {(result.EndDate - result.StartDate).Days} days");
        sb.AppendLine($"Execution Time:    {result.Duration.TotalSeconds:F2}s");
        sb.AppendLine();

        // Capital section
        sb.AppendLine("CAPITAL");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine($"Initial Capital:   ${result.InitialCapital:N2}");
        sb.AppendLine($"Final Equity:      ${result.FinalEquity:N2}");
        sb.AppendLine($"Total P&L:         {FormatPnL(result.TotalPnL)}");
        sb.AppendLine($"Total Return:      {FormatPercent(result.TotalReturn)}");
        sb.AppendLine();

        // Performance metrics section
        var perf = result.Performance;
        sb.AppendLine("PERFORMANCE METRICS");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine($"Annualized Return: {FormatPercent(perf.AnnualizedReturn)}");
        sb.AppendLine($"Sharpe Ratio:      {perf.SharpeRatio:F2}");
        sb.AppendLine($"Sortino Ratio:     {perf.SortinoRatio:F2}");
        sb.AppendLine($"Calmar Ratio:      {perf.CalmarRatio:F2}");
        sb.AppendLine($"Max Drawdown:      {FormatPercent(perf.MaxDrawdown)}");
        sb.AppendLine($"Profit Factor:     {perf.ProfitFactor:F2}");
        sb.AppendLine();

        // Trade statistics section
        sb.AppendLine("TRADE STATISTICS");
        sb.AppendLine("───────────────────────────────────────────────────────────────");
        sb.AppendLine($"Total Trades:      {perf.TotalTrades}");
        sb.AppendLine($"Winning Trades:    {perf.WinningTrades} ({GetWinRate(perf):F1}%)");
        sb.AppendLine($"Losing Trades:     {perf.LosingTrades} ({100 - GetWinRate(perf):F1}%)");
        sb.AppendLine($"Average Win:       ${perf.AverageWin:N2}");
        sb.AppendLine($"Average Loss:      ${perf.AverageLoss:N2}");
        sb.AppendLine($"Largest Win:       ${GetLargestWin(result.Trades):N2}");
        sb.AppendLine($"Largest Loss:      ${GetLargestLoss(result.Trades):N2}");
        sb.AppendLine();

        // Recent trades section (last 10)
        if (result.Trades.Count > 0)
        {
            sb.AppendLine("RECENT TRADES (Last 10)");
            sb.AppendLine("───────────────────────────────────────────────────────────────");
            sb.AppendLine("Date       | Side | Qty   | Entry   | Exit    | P&L");
            sb.AppendLine("-----------|------|-------|---------|---------|------------");

            foreach (var trade in result.Trades.TakeLast(10))
            {
                sb.AppendLine($"{trade.ExitTime:yyyy-MM-dd} | {trade.Side,-4} | {trade.Quantity,5:F2} | ${trade.EntryPrice,6:F2} | ${trade.ExitPrice,6:F2} | {FormatPnL(trade.RealizedPnL)}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("═══════════════════════════════════════════════════════════════");

        return sb.ToString();
    }

    /// <summary>
    /// Generates a JSON report.
    /// </summary>
    /// <param name="result">The backtest result.</param>
    /// <returns>JSON report string.</returns>
    public string GenerateJsonReport(BacktestResult result)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        return JsonSerializer.Serialize(result, options);
    }

    /// <summary>
    /// Generates a CSV report of all trades.
    /// </summary>
    /// <param name="result">The backtest result.</param>
    /// <returns>CSV report string.</returns>
    public string GenerateCsvReport(BacktestResult result)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("TradeId,Symbol,Side,Quantity,EntryPrice,ExitPrice,EntryTime,ExitTime,Commission,RealizedPnL,Strategy");

        // Data rows
        foreach (var trade in result.Trades)
        {
            sb.AppendLine($"{trade.Id},{trade.Symbol},{trade.Side},{trade.Quantity},{trade.EntryPrice},{trade.ExitPrice},{trade.EntryTime:yyyy-MM-dd HH:mm:ss},{trade.ExitTime:yyyy-MM-dd HH:mm:ss},{trade.Commission},{trade.RealizedPnL},{trade.StrategyName}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates an HTML report with embedded charts.
    /// </summary>
    /// <param name="result">The backtest result.</param>
    /// <returns>HTML report string.</returns>
    public string GenerateHtmlReport(BacktestResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>Backtest Report - {result.BacktestId}</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine(GetHtmlStyles());
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");
        sb.AppendLine($"        <h1>Backtest Report</h1>");
        sb.AppendLine($"        <p class=\"subtitle\">Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>");

        // Summary section
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>Summary</h2>");
        sb.AppendLine("            <table>");
        sb.AppendLine($"                <tr><td>Backtest ID</td><td>{result.BacktestId}</td></tr>");
        sb.AppendLine($"                <tr><td>Strategy</td><td>{result.StrategyName}</td></tr>");
        sb.AppendLine($"                <tr><td>Symbol</td><td>{result.Symbol}</td></tr>");
        sb.AppendLine($"                <tr><td>Period</td><td>{result.StartDate:yyyy-MM-dd} to {result.EndDate:yyyy-MM-dd}</td></tr>");
        sb.AppendLine($"                <tr><td>Duration</td><td>{(result.EndDate - result.StartDate).Days} days</td></tr>");
        sb.AppendLine($"                <tr><td>Execution Time</td><td>{result.Duration.TotalSeconds:F2}s</td></tr>");
        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        // Capital section
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>Capital</h2>");
        sb.AppendLine("            <table>");
        sb.AppendLine($"                <tr><td>Initial Capital</td><td>${result.InitialCapital:N2}</td></tr>");
        sb.AppendLine($"                <tr><td>Final Equity</td><td>${result.FinalEquity:N2}</td></tr>");
        sb.AppendLine($"                <tr><td>Total P&L</td><td class=\"{GetPnLClass(result.TotalPnL)}\">{FormatPnL(result.TotalPnL)}</td></tr>");
        sb.AppendLine($"                <tr><td>Total Return</td><td class=\"{GetPnLClass(result.TotalReturn)}\">{FormatPercent(result.TotalReturn)}</td></tr>");
        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        // Performance metrics section
        var perf = result.Performance;
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>Performance Metrics</h2>");
        sb.AppendLine("            <table>");
        sb.AppendLine($"                <tr><td>Annualized Return</td><td>{FormatPercent(perf.AnnualizedReturn)}</td></tr>");
        sb.AppendLine($"                <tr><td>Sharpe Ratio</td><td>{perf.SharpeRatio:F2}</td></tr>");
        sb.AppendLine($"                <tr><td>Sortino Ratio</td><td>{perf.SortinoRatio:F2}</td></tr>");
        sb.AppendLine($"                <tr><td>Calmar Ratio</td><td>{perf.CalmarRatio:F2}</td></tr>");
        sb.AppendLine($"                <tr><td>Max Drawdown</td><td class=\"loss\">{FormatPercent(perf.MaxDrawdown)}</td></tr>");
        sb.AppendLine($"                <tr><td>Profit Factor</td><td>{perf.ProfitFactor:F2}</td></tr>");
        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        // Trade statistics section
        sb.AppendLine("        <div class=\"section\">");
        sb.AppendLine("            <h2>Trade Statistics</h2>");
        sb.AppendLine("            <table>");
        sb.AppendLine($"                <tr><td>Total Trades</td><td>{perf.TotalTrades}</td></tr>");
        sb.AppendLine($"                <tr><td>Winning Trades</td><td class=\"profit\">{perf.WinningTrades} ({GetWinRate(perf):F1}%)</td></tr>");
        sb.AppendLine($"                <tr><td>Losing Trades</td><td class=\"loss\">{perf.LosingTrades} ({100 - GetWinRate(perf):F1}%)</td></tr>");
        sb.AppendLine($"                <tr><td>Average Win</td><td>${perf.AverageWin:N2}</td></tr>");
        sb.AppendLine($"                <tr><td>Average Loss</td><td>${perf.AverageLoss:N2}</td></tr>");
        sb.AppendLine($"                <tr><td>Largest Win</td><td>${GetLargestWin(result.Trades):N2}</td></tr>");
        sb.AppendLine($"                <tr><td>Largest Loss</td><td>${GetLargestLoss(result.Trades):N2}</td></tr>");
        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");

        // Trade list section
        if (result.Trades.Count > 0)
        {
            sb.AppendLine("        <div class=\"section\">");
            sb.AppendLine("            <h2>All Trades</h2>");
            sb.AppendLine("            <table>");
            sb.AppendLine("                <tr><th>Date</th><th>Side</th><th>Qty</th><th>Entry</th><th>Exit</th><th>P&L</th></tr>");

            foreach (var trade in result.Trades)
            {
                sb.AppendLine($"                <tr><td>{trade.ExitTime:yyyy-MM-dd}</td><td>{trade.Side}</td><td>{trade.Quantity:F2}</td><td>${trade.EntryPrice:F2}</td><td>${trade.ExitPrice:F2}</td><td class=\"{GetPnLClass(trade.RealizedPnL)}\">{FormatPnL(trade.RealizedPnL)}</td></tr>");
            }

            sb.AppendLine("            </table>");
            sb.AppendLine("        </div>");
        }

        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// Saves a report to a file.
    /// </summary>
    /// <param name="content">Report content.</param>
    /// <param name="filePath">File path to save to.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SaveReportAsync(string content, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(filePath, content);
    }

    private static string FormatPnL(decimal value)
    {
        var sign = value >= 0 ? "+" : string.Empty;
        return $"{sign}${value:N2}";
    }

    private static string FormatPercent(decimal value)
    {
        var sign = value >= 0 ? "+" : string.Empty;
        return $"{sign}{value:F2}%";
    }

    private static decimal GetWinRate(Core.Models.Portfolio.PerformanceMetrics perf)
    {
        return perf.TotalTrades > 0
            ? ((decimal)perf.WinningTrades / perf.TotalTrades) * 100m
            : 0m;
    }

    private static decimal GetLargestWin(List<Core.Models.Trading.Trade> trades)
    {
        return trades.Count > 0
            ? trades.Max(t => t.RealizedPnL)
            : 0m;
    }

    private static decimal GetLargestLoss(List<Core.Models.Trading.Trade> trades)
    {
        return trades.Count > 0
            ? trades.Min(t => t.RealizedPnL)
            : 0m;
    }

    private static string GetPnLClass(decimal value)
    {
        return value >= 0 ? "profit" : "loss";
    }

    private static string GetHtmlStyles()
    {
        return @"
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 1200px;
            margin: 0 auto;
            padding: 20px;
            background: #f5f5f5;
        }
        .container {
            background: white;
            padding: 40px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        h1 {
            color: #2c3e50;
            margin-bottom: 10px;
            border-bottom: 3px solid #3498db;
            padding-bottom: 10px;
        }
        h2 {
            color: #34495e;
            margin-top: 30px;
            margin-bottom: 15px;
            border-bottom: 1px solid #ecf0f1;
            padding-bottom: 5px;
        }
        .subtitle {
            color: #7f8c8d;
            margin-top: 0;
        }
        .section {
            margin-bottom: 30px;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 10px;
        }
        th, td {
            padding: 12px;
            text-align: left;
            border-bottom: 1px solid #ecf0f1;
        }
        th {
            background-color: #34495e;
            color: white;
            font-weight: 600;
        }
        tr:hover {
            background-color: #f8f9fa;
        }
        .profit {
            color: #27ae60;
            font-weight: 600;
        }
        .loss {
            color: #e74c3c;
            font-weight: 600;
        }";
    }
}
