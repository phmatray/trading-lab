using TradyStrat.Domain.Exceptions;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Fx;
using TradyStrat.Features.Settings.UseCases;

namespace TradyStrat.Features.PriceFeed;

public sealed partial class PriceFeedHostedService(
    IServiceProvider services,
    ILogger<PriceFeedHostedService> log) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var price       = scope.ServiceProvider.GetRequiredService<DailyPriceCache>();
        var fx          = scope.ServiceProvider.GetRequiredService<DailyFxCache>();
        var listUseCase = scope.ServiceProvider.GetRequiredService<ListInstrumentsUseCase>();

        var instruments = await listUseCase.ExecuteAsync(Unit.Value, cancellationToken);

        foreach (var inst in instruments)
            await SafeWarmPriceAsync(price, inst.Ticker, cancellationToken);

        var quotes = instruments
            .Where(i => !string.Equals(i.Currency, "EUR", StringComparison.OrdinalIgnoreCase))
            .Select(i => i.Currency.ToUpperInvariant())
            .Distinct();

        foreach (var quote in quotes)
            await SafeWarmFxAsync(fx, "EUR", quote, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SafeWarmPriceAsync(DailyPriceCache cache, string ticker, CancellationToken ct)
    {
        try { await cache.EnsureFreshAsync(ticker, ct); }
        catch (PriceFeedUnavailableException ex) { LogPriceWarmFailed(log, ex, ticker); }
    }

    private async Task SafeWarmFxAsync(DailyFxCache cache, string @base, string quote, CancellationToken ct)
    {
        try { await cache.EnsureFreshAsync(@base, quote, ct); }
        catch (FxRateUnavailableException ex) { LogFxWarmFailed(log, ex, @base, quote); }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Price warm failed for {Ticker}")]
    private static partial void LogPriceWarmFailed(ILogger logger, Exception ex, string ticker);

    [LoggerMessage(Level = LogLevel.Warning, Message = "FX warm failed for {Base}/{Quote}")]
    private static partial void LogFxWarmFailed(ILogger logger, Exception ex, string @base, string quote);
}
