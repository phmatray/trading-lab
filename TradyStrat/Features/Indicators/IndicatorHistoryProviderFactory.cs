using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.Indicators;

public sealed class IndicatorHistoryProviderFactory(
    IEnumerable<IIndicatorHistoryProvider> providers) : IIndicatorHistoryProviderFactory
{
    private readonly Dictionary<IndicatorKind, IIndicatorHistoryProvider> _byKind =
        providers.ToDictionary(p => p.Kind);

    public IIndicatorHistoryProvider For(IndicatorKind kind)
        => _byKind.TryGetValue(kind, out var p)
            ? p
            : throw new IndicatorComputationException(
                $"No history provider registered for {kind}.");
}
