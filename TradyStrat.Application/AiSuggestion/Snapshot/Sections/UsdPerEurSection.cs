using TradyStrat.Application.Fx;
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

public sealed class UsdPerEurSection(FxConverter fx) : ISnapshotSectionProvider
{
    public int Order => 70;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        try
        {
            var oneUsdInEur = await fx.ToEurAsync(1m, "USD", asOf, ct);
            if (oneUsdInEur != 0m) builder.UsdPerEur = 1m / oneUsdInEur;
        }
        catch (FxRateUnavailableException)
        {
            // Tolerant — snapshot can be built without the FX rate present.
        }
    }
}
