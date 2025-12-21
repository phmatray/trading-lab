using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing the comparison between two strategy variants.
/// Contains both backtest results and ranking information.
/// </summary>
public record StrategyComparison(
    StrategyVariant VariantA,
    BacktestResult ResultA,
    StrategyVariant VariantB,
    BacktestResult ResultB,
    ComparisonRanking Ranking,
    string Ticker,
    DateTime ComparisonDate)
{
    /// <summary>
    /// Gets the winner based on ranking (1 = Variant A, 2 = Variant B, 0 = tie).
    /// </summary>
    public int Winner => Ranking.WinnerIndex;

    /// <summary>
    /// Gets the winning variant, or null if tie.
    /// </summary>
    public StrategyVariant? WinningVariant => Winner switch
    {
        1 => VariantA,
        2 => VariantB,
        _ => null
    };

    /// <summary>
    /// Gets the winning result, or null if tie.
    /// </summary>
    public BacktestResult? WinningResult => Winner switch
    {
        1 => ResultA,
        2 => ResultB,
        _ => null
    };
}
