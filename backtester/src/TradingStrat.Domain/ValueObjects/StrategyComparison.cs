using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Immutable value object representing the comparison between two strategy variants.
/// Contains both backtest results and ranking information.
/// </summary>
public sealed class StrategyComparison : ValueObject
{
    public StrategyVariant VariantA { get; init; }
    public BacktestResult ResultA { get; init; }
    public StrategyVariant VariantB { get; init; }
    public BacktestResult ResultB { get; init; }
    public ComparisonRanking Ranking { get; init; }
    public string Ticker { get; init; }
    public DateTime ComparisonDate { get; init; }

    public StrategyComparison(
        StrategyVariant variantA,
        BacktestResult resultA,
        StrategyVariant variantB,
        BacktestResult resultB,
        ComparisonRanking ranking,
        string ticker,
        DateTime comparisonDate)
    {
        VariantA = variantA;
        ResultA = resultA;
        VariantB = variantB;
        ResultB = resultB;
        Ranking = ranking;
        Ticker = ticker;
        ComparisonDate = comparisonDate;
    }

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

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return VariantA;
        yield return ResultA;
        yield return VariantB;
        yield return ResultB;
        yield return Ranking;
        yield return Ticker;
        yield return ComparisonDate;
    }
}
