using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

public sealed class PortfolioSection(
    IPortfolioRepository portfolios,
    ListInstrumentsUseCase listInstruments) : ISnapshotSectionProvider
{
    public int Order => 30;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        if (builder.Goal is null)
            throw new InvalidOperationException("GoalSection must run before PortfolioSection");

        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var instrumentById = instruments.ToDictionary(i => i.Id, i => i);

        // Build per-instrument price map from the tickers TickersSection already populated.
        var priceMap = new Dictionary<InstrumentId, Price>();
        foreach (var inst in instruments.Where(i => i.Kind == InstrumentKind.Held))
        {
            var ctx = builder.Tickers.SingleOrDefault(t => t.Ticker == inst.Ticker);
            var priceEur = ctx?.PriceEur ?? ctx?.PriceNative ?? 0m;
            priceMap[inst.Id] = Price.Of(Money.Of(priceEur, Currency.Eur));
        }

        var portfolio = await portfolios.GetAsync(ct);
        builder.Portfolio = portfolio.SnapshotAsOf(
            asOf, instrumentById, priceMap,
            builder.Goal.Target);
    }
}
