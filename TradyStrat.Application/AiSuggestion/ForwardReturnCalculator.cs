using TradyStrat.Application.Settings;
using TradyStrat.Domain.PriceFeed;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion;

/// <summary>
/// Computes the 5-trading-bar forward return for a past AI suggestion.
/// Returns <see langword="null"/> when the forward window is not yet complete
/// (fewer than 5 bars exist after the suggestion date).
/// </summary>
public sealed class ForwardReturnCalculator(
    IPriceBarReadRepository barRepo,
    IInstrumentRepository instrumentRepo)
{
    private const int ForwardBars = 5;

    public async Task<decimal?> ComputeAsync(Suggestion suggestion, CancellationToken ct)
    {
        var instrument = await instrumentRepo.GetAsync(suggestion.InstrumentId, ct);
        if (instrument is null) return null;

        return await ComputeAsync(instrument.Ticker, suggestion.ForDate, ct);
    }

    public async Task<decimal?> ComputeAsync(string ticker, DateOnly forDate, CancellationToken ct)
    {
        var bars = await barRepo.ListSinceAsync(ticker, forDate, ct);
        if (bars.Count < 1) return null;

        var window = bars.Take(ForwardBars + 1).ToArray();
        if (window.Length < ForwardBars + 1) return null;

        var closeAt = window[0].Close;
        if (closeAt == 0m) return null;

        var fwdBar = window[ForwardBars];
        return (fwdBar.Close - closeAt) / closeAt * 100m;
    }
}
