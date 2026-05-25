using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Portfolio.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Portfolio;

public sealed class Portfolio : AggregateRoot<PortfolioId>
{
    private readonly List<Position> _positions = new();
    public IReadOnlyList<Position> Positions => _positions;

    private Portfolio() { }   // EF
    private Portfolio(PortfolioId id) : base(id) { }

    /// <summary>
    /// Creates an empty portfolio AR. The <paramref name="now"/> parameter
    /// stamps the PortfolioCreated event — pass clock.UtcNow() at call sites.
    /// </summary>
    public static Portfolio Empty(PortfolioId id, DateTime now)
    {
        var p = new Portfolio(id);
        p.Raise(new PortfolioCreated(id, now));
        return p;
    }

    /// <summary>Rehydration factory — does not raise. Used by EF reconstitution paths and snapshot replay.</summary>
    public static Portfolio Existing(PortfolioId id) => new(id);

    public Events.TradeRecorded RecordTrade(
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

        trade.AssignId(new TradeId(NextTradeIdValue()));
        var realizedDelta = position.Record(trade);

        if (created)
            Raise(new PositionOpened(position.Id, instrumentId, now));

        var evt = new Events.TradeRecorded(trade.Id, position.Id, realizedDelta, now);
        Raise(evt);
        return evt;
    }

    private int NextTradeIdValue()
    {
        var max = 0;
        foreach (var p in _positions)
            foreach (var t in p.Trades)
                if (t.Id.Value > max) max = t.Id.Value;
        return max + 1;
    }

    public Events.TradeDeleted DeleteTrade(TradeId tradeId, DateTime now)
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

        var evt = new Events.TradeDeleted(tradeId, target.Id, target.RealizedPnL - realizedBefore, now);
        Raise(evt);
        return evt;
    }

    public IReadOnlyList<Events.TradeRecorded> ImportTrades(
        IReadOnlyList<TradeDraft> drafts, DateTime now)
    {
        var savedPositions = _positions.ToList();
        var saved = _positions.ToDictionary(p => p,
            p => (lots: p.OpenLots.ToList(),
                  trades: p.Trades.ToList(),
                  realized: p.RealizedPnL));

        var results = new List<Events.TradeRecorded>(drafts.Count);
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
            ClearDomainEvents();   // discard events from the failed batch
            throw;
        }
    }

    public PortfolioSnapshot Snapshot(
        IReadOnlyDictionary<InstrumentId, Instrument> instrumentById,
        IReadOnlyDictionary<InstrumentId, Price>      priceByInstrument,
        Money goalTarget)
        => BuildSnapshot(_positions, instrumentById, priceByInstrument, goalTarget);

    public PortfolioSnapshot SnapshotAsOf(
        DateOnly asOf,
        IReadOnlyDictionary<InstrumentId, Instrument> instrumentById,
        IReadOnlyDictionary<InstrumentId, Price>      priceByInstrument,
        Money goalTarget)
    {
        var tempPortfolio = new Portfolio(Id);   // rehydration shape — no event
        foreach (var pos in _positions)
        {
            var tradesInWindow = pos.Trades
                .Where(t => t.ExecutedOn <= asOf)
                .OrderBy(t => t.ExecutedOn);
            foreach (var t in tradesInWindow)
                tempPortfolio.RecordTrade(
                    pos.InstrumentId, t.ExecutedOn, t.Side,
                    t.Quantity, t.PricePerShare, t.Fees, t.Note, t.CreatedAt);
        }
        tempPortfolio.ClearDomainEvents();   // discard events from snapshot replay
        return BuildSnapshot(tempPortfolio._positions, instrumentById, priceByInstrument, goalTarget);
    }

    public bool RehydrateLots()
    {
        var any = false;
        foreach (var pos in _positions)
        {
            if (pos.Trades.Count == 0 || pos.OpenLots.Count > 0) continue;
            var ordered = pos.Trades.OrderBy(t => t.ExecutedOn).ToList();
            pos.ClearAllForReplay();
            foreach (var t in ordered) pos.Record(t);
            any = true;
        }
        return any;
    }

    public IReadOnlyList<GrowthPoint> GrowthSeries(
        IReadOnlyDictionary<InstrumentId, IReadOnlyList<PriceBar>> barsByInstrument)
    {
        var allTrades = _positions.SelectMany(p => p.Trades.Select(t => (p.InstrumentId, t)))
                                  .OrderBy(x => x.t.ExecutedOn)
                                  .ToList();
        if (allTrades.Count == 0) return [];

        var allBarDates = barsByInstrument.Values
            .SelectMany(bars => bars.Select(b => b.Date))
            .Distinct()
            .OrderBy(d => d)
            .ToList();
        if (allBarDates.Count == 0) return [];

        var firstTradeDate = allTrades[0].t.ExecutedOn;
        var points = new List<GrowthPoint>(allBarDates.Count + 1)
        {
            new(firstTradeDate.AddDays(-1), Money.Zero(Currency.Eur), Percentage.Empty),
        };

        var sharesByInstrument = new Dictionary<InstrumentId, decimal>();
        var tradesByDate = allTrades
            .GroupBy(x => x.t.ExecutedOn)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var date in allBarDates)
        {
            if (tradesByDate.TryGetValue(date, out var todays))
                foreach (var (iid, t) in todays)
                {
                    var delta = t.IsBuy ? t.Quantity.Value : -t.Quantity.Value;
                    sharesByInstrument[iid] = sharesByInstrument.GetValueOrDefault(iid) + delta;
                }

            var totalValue = 0m;
            foreach (var (iid, bars) in barsByInstrument)
            {
                var bar = bars.FirstOrDefault(b => b.Date == date);
                if (bar is null) continue;
                var shares = sharesByInstrument.GetValueOrDefault(iid);
                totalValue += shares * bar.Close;
            }
            points.Add(new GrowthPoint(date, Money.Of(totalValue, Currency.Eur), Percentage.Empty));
        }

        return points;
    }

    private static PortfolioSnapshot BuildSnapshot(
        List<Position> positions,
        IReadOnlyDictionary<InstrumentId, Instrument> instrumentById,
        IReadOnlyDictionary<InstrumentId, Price>      priceByInstrument,
        Money goalTarget)
    {
        var rows = new List<PositionRow>(positions.Count);

        foreach (var pos in positions)
        {
            var hasInst  = instrumentById.TryGetValue(pos.InstrumentId, out var inst);
            var hasPrice = priceByInstrument.TryGetValue(pos.InstrumentId, out var price);

            var ticker   = hasInst  ? inst!.Ticker        : "?";
            var currency = hasInst  ? inst!.Currency.Code : "?";

            var qty = pos.TotalQuantity;
            var costBasis = pos.CostBasis;
            var marketValue = hasPrice
                ? (price! * qty)
                : Money.Zero(Currency.Eur);
            var unrealised = marketValue - costBasis;

            rows.Add(new PositionRow(
                InstrumentId: pos.InstrumentId,
                Ticker:        ticker,
                Currency:      currency,
                Quantity:      qty,
                CostBasisEur:  costBasis,
                MarketValueEur: marketValue,
                UnrealizedPnLEur: unrealised,
                RealizedPnLEur:   pos.RealizedPnL));
        }

        var totalValue  = rows.Aggregate(Money.Zero(Currency.Eur), (a, r) => a + r.MarketValueEur);
        var totalCost   = rows.Aggregate(Money.Zero(Currency.Eur), (a, r) => a + r.CostBasisEur);
        var totalUnreal = totalValue - totalCost;
        var totalReal   = rows.Aggregate(Money.Zero(Currency.Eur), (a, r) => a + r.RealizedPnLEur);

        var pct = goalTarget.Amount == 0m
            ? 0m
            : totalValue.Amount / goalTarget.Amount * 100m;

        var legacyShares = rows.Count == 1 ? rows[0].Quantity.Value : 0m;
        var legacyAvgCost = (rows.Count == 1 && legacyShares > 0m)
            ? Money.Of(rows[0].CostBasisEur.Amount / legacyShares, Currency.Eur)
            : Money.Zero(Currency.Eur);

        return new PortfolioSnapshot(
            Positions:        rows,
            CurrentValueEur:  totalValue,
            CostBasisEur:     totalCost,
            UnrealizedPnLEur: totalUnreal,
            RealizedPnLEur:   totalReal,
            ProgressPct:      pct,
            Shares:           legacyShares,
            AvgCostEur:       legacyAvgCost);
    }
}
