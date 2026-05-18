using TradyStrat.Application.UseCases;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Settings.UseCases;

namespace TradyStrat.Features.PriceFeed.UseCases;

public sealed class RefreshAllPricesUseCase(
    DailyPriceCache prices, DailyFxCache fx,
    ListInstrumentsUseCase listInstruments,
    ILogger<RefreshAllPricesUseCase> log)
    : UseCaseBase<Unit, Unit>(log)
{
    protected override async Task<Unit> ExecuteCore(Unit _, CancellationToken ct)
    {
        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);

        foreach (var inst in instruments)
            await prices.EnsureFreshAsync(inst.Ticker, ct);

        var quotes = instruments
            .Where(i => !string.Equals(i.Currency, "EUR", StringComparison.OrdinalIgnoreCase))
            .Select(i => i.Currency.ToUpperInvariant())
            .Distinct();

        foreach (var quote in quotes)
            await fx.EnsureFreshAsync("EUR", quote, ct);

        return Unit.Value;
    }
}
