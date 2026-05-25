using TradyStrat.Infrastructure.Fx;
using TradyStrat.Application.UseCases;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;

namespace TradyStrat.Infrastructure.PriceFeed.UseCases;

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
            .Where(i => i.Currency != Currency.Eur)
            .Select(i => i.Currency.Code)
            .Distinct();

        foreach (var quote in quotes)
            await fx.EnsureFreshAsync("EUR", quote, ct);

        return Unit.Value;
    }
}
