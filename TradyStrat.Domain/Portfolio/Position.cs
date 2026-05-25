using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed class Position : Entity<PositionId>
{
    public InstrumentId InstrumentId { get; private set; }

    private readonly List<Lot>   _openLots = new();
    private readonly List<Trade> _trades   = new();

    private Money _realizedPnL = Money.Zero(Currency.Eur);

    public IReadOnlyList<Lot>   OpenLots    => _openLots;
    public IReadOnlyList<Trade> Trades      => _trades;
    public Money                RealizedPnL => _realizedPnL;

    public Quantity TotalQuantity
    {
        get
        {
            var sum = Quantity.Zero;
            foreach (var lot in _openLots)
                sum += lot.Quantity;
            return sum;
        }
    }

    public Money CostBasis
    {
        get
        {
            var sum = Money.Zero(Currency.Eur);
            foreach (var lot in _openLots)
                sum += lot.CostBasis;
            return sum;
        }
    }

    private Position() { }   // EF

    private Position(InstrumentId instrumentId) : base(PositionId.New())
    {
        InstrumentId = instrumentId;
    }

    public static Position OpenFor(InstrumentId instrumentId) => new(instrumentId);

    public Money Record(Trade trade)
    {
        var realizedBefore = _realizedPnL;
        if (trade.Id == TradeId.New())
            trade.AssignId(new TradeId(_trades.Count + 1));
        _trades.Add(trade);

        if (trade.IsBuy)
        {
            var grossPlusFees = trade.PricePerShare * trade.Quantity + trade.Fees;
            var unitCost = grossPlusFees / trade.Quantity.Value;
            _openLots.Add(new Lot(trade.ExecutedOn, trade.Quantity, unitCost));
            return Money.Zero(Currency.Eur);
        }

        var remaining = trade.Quantity.Value;
        var totalSellQty = trade.Quantity.Value;
        while (remaining > 0m)
        {
            if (_openLots.Count == 0)
                throw new Exceptions.TradeValidationException(
                    $"Sell on {trade.ExecutedOn} for instrument {InstrumentId} exceeds open lots.");

            var head = _openLots[0];
            var consumed = Math.Min(head.Quantity.Value, remaining);

            var pricePnL = (trade.PricePerShare.PerUnit - head.UnitCost) * consumed;
            var feeShare = trade.Fees * (consumed / totalSellQty);
            _realizedPnL = _realizedPnL + pricePnL - feeShare;

            if (consumed == head.Quantity.Value)
                _openLots.RemoveAt(0);
            else
                _openLots[0] = head.WithQuantity(Quantity.Of(head.Quantity.Value - consumed));

            remaining -= consumed;
        }

        return _realizedPnL - realizedBefore;
    }

    internal void ClearAllForReplay()
    {
        _openLots.Clear();
        _trades.Clear();
        _realizedPnL = Money.Zero(Currency.Eur);
    }

    internal void RestoreState(IEnumerable<Lot> lots, IEnumerable<Trade> trades, Money realized)
    {
        _openLots.Clear(); _openLots.AddRange(lots);
        _trades.Clear();   _trades.AddRange(trades);
        _realizedPnL = realized;
    }
}
