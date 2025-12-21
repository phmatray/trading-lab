using TradingStrat.Domain.Entities;

namespace TradingStrat.Web.Services;

public class ChartDataService
{
    /// <summary>
    /// Prepares equity curve data for chart rendering
    /// </summary>
    public List<EquityPoint> PrepareEquityCurveData(List<EquityPoint> equityCurve)
    {
        return equityCurve ?? new List<EquityPoint>();
    }

    /// <summary>
    /// Formats performance metrics for display
    /// </summary>
    public Dictionary<string, string> FormatMetrics(PerformanceMetrics? metrics)
    {
        if (metrics == null)
        {
            return new Dictionary<string, string>();
        }

        return new Dictionary<string, string>
        {
            ["Total Return"] = $"{metrics.TotalReturnPercentage:F2}%",
            ["Sharpe Ratio"] = metrics.SharpeRatio.ToString("F2"),
            ["Max Drawdown"] = $"{metrics.MaxDrawdownPercentage:F2}%",
            ["Win Rate"] = $"{metrics.WinRate:F2}%"
        };
    }
}
