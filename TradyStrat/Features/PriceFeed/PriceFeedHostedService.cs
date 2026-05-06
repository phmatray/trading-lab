using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradyStrat.Features.Fx;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.PriceFeed;

public sealed partial class PriceFeedHostedService(
    IServiceProvider services,
    ILogger<PriceFeedHostedService> log) : IHostedService
{
    private static readonly string[] Tickers = ["CON3.DE", "COIN", "BTC-USD"];
    private const string FxPair = "EURUSD";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var price = scope.ServiceProvider.GetRequiredService<DailyPriceCache>();
        var fx    = scope.ServiceProvider.GetRequiredService<DailyFxCache>();

        foreach (var t in Tickers)
            await SafeWarmPriceAsync(price, t, cancellationToken);

        await SafeWarmFxAsync(fx, FxPair, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SafeWarmPriceAsync(DailyPriceCache cache, string ticker, CancellationToken ct)
    {
        try { await cache.EnsureFreshAsync(ticker, ct); }
        catch (PriceFeedUnavailableException ex) { LogPriceWarmFailed(log, ex, ticker); }
    }

    private async Task SafeWarmFxAsync(DailyFxCache cache, string pair, CancellationToken ct)
    {
        try { await cache.EnsureFreshAsync(pair, ct); }
        catch (FxRateUnavailableException ex) { LogFxWarmFailed(log, ex, pair); }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Price warm failed for {Ticker}")]
    private static partial void LogPriceWarmFailed(ILogger logger, Exception ex, string ticker);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FX warm failed for {Pair}")]
    private static partial void LogFxWarmFailed(ILogger logger, Exception ex, string pair);
}
