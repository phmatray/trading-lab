using TradingStrat.Domain.Common;

namespace TradingStrat.Domain.Entities;

public enum TradeType
{
    Buy,
    Sell
}

public class Trade
{
    private decimal _price;
    private int _quantity;
    private decimal _commission;

    public int Id { get; set; }
    public int? BacktestRunId { get; set; }
    public DateTime DateTime { get; set; }
    public TradeType Type { get; set; }

    /// <summary>
    /// Gets or sets the price per share for this trade.
    /// Must be greater than zero.
    /// </summary>
    public decimal Price
    {
        get => _price;
        set
        {
            ValidationGuard.Require(value).GreaterThan(0m);
            _price = value;
        }
    }

    /// <summary>
    /// Gets or sets the number of shares traded.
    /// Must be greater than zero.
    /// </summary>
    public int Quantity
    {
        get => _quantity;
        set
        {
            ValidationGuard.Require(value).GreaterThan(0);
            _quantity = value;
        }
    }

    /// <summary>
    /// Gets or sets the commission charged for this trade.
    /// Must be non-negative (zero or positive).
    /// </summary>
    public decimal Commission
    {
        get => _commission;
        set
        {
            ValidationGuard.Require(value).GreaterThanOrEqual(0m);
            _commission = value;
        }
    }

    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string? Reason { get; set; }
    public decimal? ProfitLoss { get; set; }
}
