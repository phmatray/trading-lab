using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

public sealed class PortfolioSection(
    PortfolioService portfolio,
    ListInstrumentsUseCase listInstruments) : ISnapshotSectionProvider
{
    public int Order => 30;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        if (builder.Goal is null)
            throw new InvalidOperationException("GoalSection must run before PortfolioSection");

        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);

        // Build per-instrument price map from the tickers TickersSection already populated.
        var priceMap = new Dictionary<int, (decimal PriceEur, string Ticker, string Currency)>();
        foreach (var inst in instruments.Where(i => i.Kind == InstrumentKind.Held))
        {
            var ctx = builder.Tickers.SingleOrDefault(t => t.Ticker == inst.Ticker);
            var priceEur = ctx?.PriceEur ?? ctx?.PriceNative ?? 0m;
            priceMap[inst.Id] = (priceEur, inst.Ticker, inst.Currency);
        }

        builder.Portfolio = await portfolio.SnapshotAsync(asOf, priceMap, builder.Goal.TargetEur, ct);
    }
}
