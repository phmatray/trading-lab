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

    // Domain Behaviors

    /// <summary>
    /// Calculates the gross amount for this trade (Price × Quantity).
    /// </summary>
    public decimal CalculateGrossAmount() => Price * Quantity;

    /// <summary>
    /// Calculates the net amount for this trade.
    /// Buy trades: -(Price × Quantity + Commission)
    /// Sell trades: Price × Quantity - Commission
    /// </summary>
    public decimal CalculateNetAmount()
    {
        decimal grossAmount = CalculateGrossAmount();
        return Type == TradeType.Buy
            ? -(grossAmount + Commission)
            : grossAmount - Commission;
    }

    /// <summary>
    /// Returns true if this is a buy trade.
    /// </summary>
    public bool IsBuyTrade() => Type == TradeType.Buy;

    /// <summary>
    /// Returns true if this is a sell trade.
    /// </summary>
    public bool IsSellTrade() => Type == TradeType.Sell;

    /// <summary>
    /// Returns true if this trade resulted in a profit (ProfitLoss > 0).
    /// Only applicable for sell trades.
    /// </summary>
    public bool IsWinningTrade() => ProfitLoss.HasValue && ProfitLoss.Value > 0;

    /// <summary>
    /// Returns true if this trade resulted in a loss (ProfitLoss &lt; 0).
    /// Only applicable for sell trades.
    /// </summary>
    public bool IsLosingTrade() => ProfitLoss.HasValue && ProfitLoss.Value < 0;

    /// <summary>
    /// Returns true if this trade broke even (ProfitLoss == 0).
    /// Only applicable for sell trades.
    /// </summary>
    public bool IsBreakEvenTrade() => ProfitLoss.HasValue && ProfitLoss.Value == 0;

    /// <summary>
    /// Gets the absolute value of the profit or loss.
    /// Returns 0 if ProfitLoss is null.
    /// </summary>
    public decimal GetAbsoluteProfitLoss() => Math.Abs(ProfitLoss ?? 0);

    /// <summary>
    /// Gets the profit or loss value, returning 0 if null.
    /// </summary>
    public decimal GetProfitLoss() => ProfitLoss ?? 0;

    /// <summary>
    /// Returns true if this trade has all required fields set for a complete trade.
    /// </summary>
    public bool IsComplete() =>
        Price > 0 &&
        Quantity > 0 &&
        Commission >= 0 &&
        DateTime != default;

    public override string ToString() =>
        $"{Type} {Quantity} @ {Price:C2} on {DateTime:yyyy-MM-dd}" +
        (ProfitLoss.HasValue ? $" (P/L: {ProfitLoss:C2})" : "");
}
