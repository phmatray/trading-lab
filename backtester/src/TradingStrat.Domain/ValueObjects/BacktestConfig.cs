using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.ValueObjects;

/// <summary>
/// Domain value object for backtest configuration (zero external dependencies).
/// </summary>
public sealed class BacktestConfig : ValueObject
{
    public string Ticker { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public decimal InitialCapital { get; init; }
    public decimal CommissionPercentage { get; init; }
    public decimal MinimumCommission { get; init; }

    public BacktestConfig(
        string ticker,
        DateTime startDate,
        DateTime endDate,
        decimal initialCapital,
        decimal commissionPercentage,
        decimal minimumCommission)
    {
        // Validate ticker
        ValidationGuard.Require(ticker).NotNullOrWhiteSpace();

        // Validate date range
        if (startDate >= endDate)
        {
            throw new ArgumentException("Start date must be before end date", nameof(startDate));
        }

        if (startDate > DateTime.Today)
        {
            throw new ArgumentException("Start date cannot be in the future", nameof(startDate));
        }

        // Validate initial capital
        ValidationGuard.Require(initialCapital).GreaterThan(0m);

        // Validate commissions
        ValidationGuard.Require(commissionPercentage).GreaterThanOrEqual(0m);
        ValidationGuard.Require(minimumCommission).GreaterThanOrEqual(0m);

        Ticker = ticker;
        StartDate = startDate;
        EndDate = endDate;
        InitialCapital = initialCapital;
        CommissionPercentage = commissionPercentage;
        MinimumCommission = minimumCommission;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Ticker;
        yield return StartDate;
        yield return EndDate;
        yield return InitialCapital;
        yield return CommissionPercentage;
        yield return MinimumCommission;
    }
}
