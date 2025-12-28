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
        string Ticker,
        DateTime StartDate,
        DateTime EndDate,
        decimal InitialCapital,
        decimal CommissionPercentage,
        decimal MinimumCommission)
    {
        // Validate ticker
        ValidationGuard.Require(Ticker).NotNullOrWhiteSpace();

        // Validate date range
        if (StartDate >= EndDate)
        {
            throw new ArgumentException("Start date must be before end date", nameof(StartDate));
        }

        if (StartDate > DateTime.Today)
        {
            throw new ArgumentException("Start date cannot be in the future", nameof(StartDate));
        }

        // Validate initial capital
        ValidationGuard.Require(InitialCapital).GreaterThan(0m);

        // Validate commissions
        ValidationGuard.Require(CommissionPercentage).GreaterThanOrEqual(0m);
        ValidationGuard.Require(MinimumCommission).GreaterThanOrEqual(0m);

        this.Ticker = Ticker;
        this.StartDate = StartDate;
        this.EndDate = EndDate;
        this.InitialCapital = InitialCapital;
        this.CommissionPercentage = CommissionPercentage;
        this.MinimumCommission = MinimumCommission;
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
