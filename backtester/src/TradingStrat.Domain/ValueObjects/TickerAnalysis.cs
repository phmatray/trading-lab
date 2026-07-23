using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// AI-generated analysis for a specific ticker.
/// Used by the AI Analysis Panel in the UI.
/// </summary>
public sealed class TickerAnalysis : ValueObject
{
    /// <summary>The ticker symbol being analyzed.</summary>
    public string Ticker { get; init; }

    /// <summary>Current market price.</summary>
    public decimal CurrentPrice { get; init; }

    /// <summary>Daily price change percentage.</summary>
    public decimal DailyChangePercent { get; init; }

    /// <summary>AI-generated summary of market conditions.</summary>
    public string Summary { get; init; }

    /// <summary>AI-generated recommendation (BUY/SELL/HOLD).</summary>
    public string Recommendation { get; init; }

    /// <summary>List of actionable insights for the user.</summary>
    public List<string> ActionableInsights { get; init; }

    /// <summary>Confidence level (0-100).</summary>
    public int Confidence { get; init; }

    /// <summary>Timestamp when the analysis was generated.</summary>
    public DateTime GeneratedAt { get; init; }

    public TickerAnalysis(
        string ticker,
        decimal currentPrice,
        decimal dailyChangePercent,
        string summary,
        string recommendation,
        List<string> actionableInsights,
        int confidence,
        DateTime generatedAt)
    {
        Ticker = ticker;
        CurrentPrice = currentPrice;
        DailyChangePercent = dailyChangePercent;
        Summary = summary;
        Recommendation = recommendation;
        ActionableInsights = actionableInsights;
        Confidence = confidence;
        GeneratedAt = generatedAt;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Ticker;
        yield return CurrentPrice;
        yield return DailyChangePercent;
        yield return Summary;
        yield return Recommendation;
        foreach (string insight in ActionableInsights)
        {
            yield return insight;
        }
        yield return Confidence;
        yield return GeneratedAt;
    }
}
