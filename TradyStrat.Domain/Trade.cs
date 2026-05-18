namespace TradyStrat.Domain;

public sealed record Trade
{
    public required int Id { get; init; }
    public required int InstrumentId { get; init; }
    public required DateOnly ExecutedOn { get; init; }
    public required TradeSide Side { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal PricePerShare { get; init; }
    public decimal FeesEur { get; init; }
    public string? Note { get; init; }
    public required DateTime CreatedAt { get; init; }

    public decimal GrossEur => Quantity * PricePerShare;
    public decimal NetEur   => Side == TradeSide.Buy ? GrossEur + FeesEur : GrossEur - FeesEur;
    public bool    IsBuy    => Side == TradeSide.Buy;
}
