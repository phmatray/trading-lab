namespace TradingStrat.Services.Backtesting;

public class BacktestBuilder
{
    private string _ticker = string.Empty;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private decimal _initialCapital = 10_000m;
    private decimal _commissionPercentage = 0.001m;
    private decimal _minimumCommission = 1.0m;
    private PositionSizingMode _positionSizing = PositionSizingMode.AllIn;
    private int? _fixedQuantity;
    private decimal? _positionPercentage;

    public BacktestBuilder ForTicker(string ticker)
    {
        _ticker = ticker;
        return this;
    }

    public BacktestBuilder WithDateRange(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
        return this;
    }

    public BacktestBuilder WithInitialCapital(decimal initialCapital)
    {
        _initialCapital = initialCapital;
        return this;
    }

    public BacktestBuilder WithCommission(decimal percentage, decimal minimum)
    {
        _commissionPercentage = percentage;
        _minimumCommission = minimum;
        return this;
    }

    public BacktestBuilder WithAllInPositionSizing()
    {
        _positionSizing = PositionSizingMode.AllIn;
        return this;
    }

    public BacktestBuilder WithFixedPositionSizing(int quantity)
    {
        _positionSizing = PositionSizingMode.Fixed;
        _fixedQuantity = quantity;
        return this;
    }

    public BacktestBuilder WithPercentagePositionSizing(decimal percentage)
    {
        _positionSizing = PositionSizingMode.Percentage;
        _positionPercentage = percentage;
        return this;
    }

    public BacktestConfiguration Build()
    {
        if (string.IsNullOrEmpty(_ticker))
            throw new InvalidOperationException("Ticker must be specified");

        if (!_startDate.HasValue || !_endDate.HasValue)
            throw new InvalidOperationException("Date range must be specified");

        if (_initialCapital <= 0)
            throw new InvalidOperationException("Initial capital must be greater than zero");

        return new BacktestConfiguration(
            _ticker,
            _startDate.Value,
            _endDate.Value,
            _initialCapital,
            _commissionPercentage,
            _minimumCommission,
            _positionSizing,
            _fixedQuantity,
            _positionPercentage
        );
    }
}
