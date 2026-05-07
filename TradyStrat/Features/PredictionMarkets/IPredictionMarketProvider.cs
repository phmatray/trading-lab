namespace TradyStrat.Features.PredictionMarkets;

public interface IPredictionMarketProvider
{
    Task<IReadOnlyList<PredictionMarket>> GetMarketsAsync(CancellationToken ct);
}
