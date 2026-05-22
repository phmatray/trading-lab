using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed class Position
{
    public PositionId   Id           { get; private set; }
    public InstrumentId InstrumentId { get; private set; }

    private readonly List<Lot>   _openLots = new();
    private readonly List<Trade> _trades   = new();

    private Money _realizedPnL = Money.Zero(Currency.Eur);

    public IReadOnlyList<Lot>   OpenLots   => _openLots;
    public IReadOnlyList<Trade> Trades     => _trades;
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

    private Position(InstrumentId instrumentId)
    {
        Id           = PositionId.New();
        InstrumentId = instrumentId;
    }

    public static Position OpenFor(InstrumentId instrumentId) => new(instrumentId);

    public Money Record(Trade trade)
    {
        var realizedBefore = _realizedPnL;
        // Assign a sequential ID within this position so the AR can identify
        // trades for DeleteTrade. EF preserves these IDs (ValueGeneratedNever).
        trade.AssignId(new TradeId(_trades.Count + 1));
        _trades.Add(trade);

        if (trade.IsBuy)
        {
            // Fold fees into cost basis: unitCost = (gross + fees) / qty
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

            // P&L from the price delta on consumed shares
            var pricePnL = (trade.PricePerShare.PerUnit - head.UnitCost) * consumed;
            // Pro-rata fee allocation on consumed shares
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
