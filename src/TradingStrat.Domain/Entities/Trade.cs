namespace TradingStrat.Domain.Entities;

public enum TradeType
{
    Buy,
    Sell
}

public class Trade
{
    public int Id { get; set; }
    public int? BacktestRunId { get; set; }
    public DateTime DateTime { get; set; }
    public TradeType Type { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Commission { get; set; }
    public decimal GrossAmount { get; set; }
    public decimal NetAmount { get; set; }
    public string? Reason { get; set; }
    public decimal? ProfitLoss { get; set; }
}
