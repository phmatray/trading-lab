namespace TradingStrat.Domain.Entities;

/// <summary>
/// Domain entity representing an AI-generated strategy analysis and recommendation.
/// Contains structured insights about a trading strategy's performance and actionable suggestions.
/// Maps to the StrategyRecommendations table in the SQLite database.
/// </summary>
public class StrategyRecommendation
{
    /// <summary>
    /// Database primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Stock ticker symbol this recommendation applies to.
    /// Required field.
    /// </summary>
    public required string Ticker { get; set; }

    /// <summary>
    /// Strategy type identifier (e.g., "ma", "rsi", "macd", "ml").
    /// Required field.
    /// </summary>
    public required string StrategyType { get; set; }

    /// <summary>
    /// Brief summary of the current market conditions and strategy performance (2-3 sentences).
    /// Required field.
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Clear buy/hold/sell recommendation with rationale.
    /// Required field.
    /// </summary>
    public required string Recommendation { get; set; }

    /// <summary>
    /// List of specific, actionable steps with priorities and confidence levels.
    /// </summary>
    public List<ActionItem> ActionItems { get; set; } = new();

    /// <summary>
    /// Overall confidence score for this recommendation (0-100).
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Timestamp when this recommendation was generated.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Represents a single actionable item within a strategy recommendation.
/// </summary>
public class ActionItem
{
    /// <summary>
    /// Description of the action to take.
    /// Required field.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Priority level: "High", "Medium", or "Low".
    /// Defaults to "Medium".
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Confidence level for this specific action (0.0-1.0).
    /// Optional field.
    /// </summary>
    public decimal? ConfidenceLevel { get; set; }
}
