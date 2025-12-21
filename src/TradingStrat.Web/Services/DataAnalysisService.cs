using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Web.Models;

namespace TradingStrat.Web.Services;

public interface IDataAnalysisService
{
    Task<DataAnalysisResult> AnalyzeDataAsync(DataSummaryResult summary);
}

public class DataAnalysisService : IDataAnalysisService
{
    public async Task<DataAnalysisResult> AnalyzeDataAsync(DataSummaryResult summary)
    {
        // Simulate AI processing delay
        await Task.Delay(500);

        // Calculate sentiment based on price position
        decimal priceRange = summary.MaxPrice!.Value - summary.MinPrice!.Value;
        decimal percentFromMin = (summary.LatestClose!.Value - summary.MinPrice.Value) / priceRange * 100;
        bool isBullish = percentFromMin > 50;

        return new DataAnalysisResult(
            Sentiment: isBullish ? "BULLISH" : "BEARISH",
            Summary: GenerateSummary(summary, percentFromMin),
            Recommendations: GenerateRecommendations(summary, isBullish, percentFromMin),
            Confidence: CalculateConfidence(summary),
            RobotImage: "/img/robot-neutral.png"
        );
    }

    private static string GenerateSummary(DataSummaryResult summary, decimal percentFromMin)
    {
        string trendDirection = percentFromMin > 50 ? "bullish" : "bearish";
        return $"Based on {summary.TotalRecords} data points from {summary.OldestDate:yyyy-MM-dd} to {summary.LatestDate:yyyy-MM-dd}, " +
               $"the trend appears {trendDirection}. " +
               $"Latest price ${summary.LatestClose:F2} is {percentFromMin:F1}% from the minimum.";
    }

    private static List<string> GenerateRecommendations(DataSummaryResult summary, bool isBullish, decimal percentFromMin)
    {
        List<string> recommendations = [];

        if (isBullish)
        {
            recommendations.Add("Consider accumulating positions on minor dips");
            recommendations.Add("Set stop-loss at recent support levels");
            recommendations.Add("Monitor for breakout above resistance");
        }
        else
        {
            recommendations.Add("Exercise caution with new positions");
            recommendations.Add("Review portfolio allocation");
            recommendations.Add("Wait for stabilization signals");
        }

        if (summary.TotalRecords < 100)
        {
            recommendations.Add("⚠️ Limited historical data - analysis may be less reliable");
        }

        return recommendations;
    }

    private static decimal CalculateConfidence(DataSummaryResult summary)
    {
        decimal confidence = 50; // Base confidence

        // More data = higher confidence
        if (summary.TotalRecords > 250)
        {
            confidence += 20;
        }
        else if (summary.TotalRecords > 100)
        {
            confidence += 10;
        }

        // Recent data = higher confidence
        int daysSinceLatest = (DateTime.UtcNow.Date - summary.LatestDate!.Value.Date).Days;
        if (daysSinceLatest <= 1)
        {
            confidence += 15;
        }
        else if (daysSinceLatest <= 7)
        {
            confidence += 10;
        }
        else if (daysSinceLatest <= 30)
        {
            confidence += 5;
        }

        // Data span = higher confidence
        int totalDays = (summary.LatestDate.Value - summary.OldestDate!.Value).Days;
        if (totalDays > 365)
        {
            confidence += 15;
        }
        else if (totalDays > 180)
        {
            confidence += 10;
        }

        return Math.Min(confidence, 95); // Cap at 95%
    }
}
