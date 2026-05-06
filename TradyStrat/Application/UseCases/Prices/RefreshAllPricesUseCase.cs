using TradyStrat.Application.Abstractions;
using TradyStrat.Features.Fx;
using TradyStrat.Features.PriceFeed;

namespace TradyStrat.Application.UseCases.Prices;

public sealed class RefreshAllPricesUseCase(
    DailyPriceCache prices, DailyFxCache fx,
    ILogger<RefreshAllPricesUseCase> log)
    : UseCaseBase<Unit, Unit>(log)
{
    private static readonly string[] Tickers = ["CON3.L", "COIN", "BTC-USD"];
    private const string FxPair = "EURUSD";

    protected override async Task<Unit> ExecuteCore(Unit _, CancellationToken ct)
    {
        foreach (var t in Tickers) await prices.EnsureFreshAsync(t, ct);
        await fx.EnsureFreshAsync(FxPair, ct);
        return Unit.Value;
    }
}
