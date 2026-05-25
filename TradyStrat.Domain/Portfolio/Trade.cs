using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared.Money;

namespace TradyStrat.Domain.Portfolio;

public sealed class Trade : Entity<TradeId>
{
    public DateOnly  ExecutedOn    { get; private set; }
    public TradeSide Side          { get; private set; }
    public Quantity  Quantity      { get; private set; } = Quantity.None;
    public Price     PricePerShare { get; private set; } = Price.None(Currency.Eur);
    public Money     Fees          { get; private set; } = Money.Zero(Currency.Eur);
    public string    Note          { get; private set; } = "";
    public DateTime  CreatedAt     { get; private set; }

    private Trade() { }   // EF

    private Trade(
        DateOnly executedOn, TradeSide side, Quantity quantity,
        Price pricePerShare, Money fees, string note, DateTime now)
        : base(TradeId.New())
    {
        ExecutedOn    = executedOn;
        Side          = side;
        Quantity      = quantity;
        PricePerShare = pricePerShare;
        Fees          = fees;
        Note          = note;
        CreatedAt     = now;
    }

    public static Trade Create(
        DateOnly executedOn, TradeSide side, Quantity quantity,
        Price pricePerShare, Money fees, string note, DateTime now)
    {
        if (!quantity.IsSpecified || quantity.Value <= 0m)
            throw new TradeValidationException("Quantity must be positive.");
        if (pricePerShare.IsEmpty || pricePerShare.PerUnit.Amount <= 0m)
            throw new TradeValidationException("Price per share must be positive.");
        if (fees.IsEmpty || fees.Amount < 0m)
            throw new TradeValidationException("Fees cannot be negative.");

        return new Trade(executedOn, side, quantity, pricePerShare, fees, note ?? "", now);
    }

    public bool IsBuy => Side == TradeSide.Buy;

    public Money Gross => PricePerShare * Quantity;
    public Money Net   => Side == TradeSide.Buy ? Gross + Fees : Gross - Fees;

    internal void AssignId(TradeId id) => Id = id;
}
