using TradyStrat.Domain.Suggestions;
namespace TradyStrat.Application.PredictionMarkets;

public interface IPredictionMarketProvider
{
    Task<IReadOnlyList<PredictionMarket>> GetMarketsAsync(CancellationToken ct);
}
