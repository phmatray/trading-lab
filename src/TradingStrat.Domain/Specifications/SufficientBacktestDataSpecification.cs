using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Domain.Specifications;

/// <summary>
/// Specification that validates a backtest has sufficient historical data.
/// </summary>
public class SufficientBacktestDataSpecification : ISpecification<BacktestConfig>
{
    private readonly int _minimumBars;

    /// <summary>
    /// Initializes a new instance of the <see cref="SufficientBacktestDataSpecification"/> class.
    /// </summary>
    /// <param name="minimumBars">Minimum number of bars (days) required for backtesting.</param>
    public SufficientBacktestDataSpecification(int minimumBars)
    {
        _minimumBars = minimumBars;
    }

    /// <summary>
    /// Gets the reason why the specification was not satisfied.
    /// </summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// Checks whether the backtest configuration has sufficient data.
    /// </summary>
    /// <param name="candidate">The backtest configuration to validate.</param>
    /// <returns>True if the date range provides sufficient data bars.</returns>
    public bool IsSatisfiedBy(BacktestConfig candidate)
    {
        Reason = string.Empty;

        if (candidate == null)
        {
            Reason = "Backtest configuration cannot be null";
            return false;
        }

        int totalDays = (candidate.EndDate - candidate.StartDate).Days;

        if (totalDays < _minimumBars)
        {
            Reason = $"Insufficient data: {totalDays} days, need {_minimumBars}";
            return false;
        }

        return true;
    }
}
