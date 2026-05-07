using TradyStrat.Common.UseCases;
using TradyStrat.Features.Fx;

namespace TradyStrat.Features.PriceFeed.UseCases;

public sealed class RefreshAllPricesUseCase(
    DailyPriceCache prices, DailyFxCache fx,
    ILogger<RefreshAllPricesUseCase> log)
    : UseCaseBase<Unit, Unit>(log)
{
    private static readonly string[] Tickers = ["CON3.L", "COIN", "BTC-USD"];

    protected override async Task<Unit> ExecuteCore(Unit _, CancellationToken ct)
    {
        foreach (var t in Tickers) await prices.EnsureFreshAsync(t, ct);
        await fx.EnsureFreshAsync("EUR", "USD", ct);
        return Unit.Value;
    }
}
