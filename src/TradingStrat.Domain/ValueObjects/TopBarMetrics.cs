using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Metrics displayed in the global top bar.
/// Immutable value object for efficient data transfer.
/// </summary>
public sealed class TopBarMetrics : ValueObject
{
    /// <summary>Total portfolio value (cash + positions).</summary>
    public decimal TotalValue { get; init; }

    /// <summary>Today's return in dollars.</summary>
    public decimal TodayReturnDollars { get; init; }

    /// <summary>Today's return as a percentage.</summary>
    public decimal TodayReturnPercentage { get; init; }

    /// <summary>Percentage of positions with unrealized gains > 0.</summary>
    public decimal WinRatePercentage { get; init; }

    /// <summary>Number of positions with unrealized gains > 0.</summary>
    public int WinningPositions { get; init; }

    /// <summary>Total number of positions.</summary>
    public int TotalPositions { get; init; }

    public TopBarMetrics(
        decimal totalValue,
        decimal todayReturnDollars,
        decimal todayReturnPercentage,
        decimal winRatePercentage,
        int winningPositions,
        int totalPositions)
    {
        TotalValue = totalValue;
        TodayReturnDollars = todayReturnDollars;
        TodayReturnPercentage = todayReturnPercentage;
        WinRatePercentage = winRatePercentage;
        WinningPositions = winningPositions;
        TotalPositions = totalPositions;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalValue;
        yield return TodayReturnDollars;
        yield return TodayReturnPercentage;
        yield return WinRatePercentage;
        yield return WinningPositions;
        yield return TotalPositions;
    }
}
