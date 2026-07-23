using TradyStrat.Domain;

namespace TradyStrat.Application.Fx;

public interface IFxRateReadRepository
{
    /// <summary>Latest FxRate for (base, quote) on or before <paramref name="asOf"/>.</summary>
    Task<FxRate?> LatestAsync(string @base, string quote, DateOnly asOf, CancellationToken ct);

    Task<IReadOnlyList<FxRate>> ListForPairAsync(string @base, string quote, CancellationToken ct);
}
