using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed class Portfolio
{
    public PortfolioId Id { get; private set; }

    private readonly List<Position> _positions = new();
    public IReadOnlyList<Position> Positions => _positions;

    private Portfolio() { }   // EF

    private Portfolio(PortfolioId id) { Id = id; }

    public static Portfolio Empty(PortfolioId id) => new(id);

    public TradeRecorded RecordTrade(
        InstrumentId instrumentId,
        DateOnly executedOn, TradeSide side,
        Quantity quantity, Price pricePerShare, Money fees, string note, DateTime now)
    {
        var trade = Trade.Create(executedOn, side, quantity, pricePerShare, fees, note, now);

        var position = _positions.FirstOrDefault(p => p.InstrumentId == instrumentId);
        var created = position is null;
        if (position is null)
        {
            position = Position.OpenFor(instrumentId);
            _positions.Add(position);
        }

        var realizedDelta = position.Record(trade);
        return new TradeRecorded(trade.Id, position.Id, created, realizedDelta);
    }

    public TradeDeleted DeleteTrade(TradeId tradeId)
    {
        Position? target = null;
        foreach (var p in _positions)
            if (p.Trades.Any(t => t.Id == tradeId)) { target = p; break; }
        if (target is null)
            throw new TradeValidationException($"Trade {tradeId} not found.");

        var realizedBefore = target.RealizedPnL;
        var remaining = target.Trades.Where(t => t.Id != tradeId)
                                     .OrderBy(t => t.ExecutedOn)
                                     .ToList();
        target.ClearAllForReplay();
        foreach (var t in remaining) target.Record(t);

        return new TradeDeleted(target.Id, target.RealizedPnL - realizedBefore);
    }

    public IReadOnlyList<TradeRecorded> ImportTrades(
        IReadOnlyList<TradeDraft> drafts, DateTime now)
    {
        var savedPositions = _positions.ToList();
        var saved = _positions.ToDictionary(p => p,
            p => (lots: p.OpenLots.ToList(),
                  trades: p.Trades.ToList(),
                  realized: p.RealizedPnL));

        var results = new List<TradeRecorded>(drafts.Count);
        try
        {
            foreach (var d in drafts)
                results.Add(RecordTrade(d.InstrumentId, d.ExecutedOn, d.Side,
                    d.Quantity, d.PricePerShare, d.Fees, d.Note, now));
            return results;
        }
        catch
        {
            _positions.Clear();
            _positions.AddRange(savedPositions);
            foreach (var p in _positions)
            {
                var (lots, trades, realized) = saved[p];
                p.RestoreState(lots, trades, realized);
            }
            throw;
        }
    }
}
