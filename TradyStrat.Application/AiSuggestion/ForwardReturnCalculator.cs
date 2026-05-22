using Ardalis.Specification;
using TradyStrat.Application.PriceFeed.Specifications;
using TradyStrat.Domain;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Application.AiSuggestion;

/// <summary>
/// Computes the 5-trading-bar forward return for a past AI suggestion.
/// Returns <see langword="null"/> when the forward window is not yet complete
/// (fewer than 5 bars exist after the suggestion date).
/// </summary>
public sealed class ForwardReturnCalculator(
    IReadRepositoryBase<PriceBar> barRepo,
    IReadRepositoryBase<Instrument> instrumentRepo)
{
    private const int ForwardBars = 5;

    /// <summary>
    /// Looks up the instrument ticker, fetches bars from the suggestion date onwards,
    /// and returns the 5-bar forward return as a percentage.
    /// Returns <see langword="null"/> if the forward window is incomplete or the instrument/bars are not found.
    /// </summary>
    public async Task<decimal?> ComputeAsync(Suggestion suggestion, CancellationToken ct)
    {
        var instrument = await instrumentRepo.GetByIdAsync(suggestion.InstrumentId.Value, ct);
        if (instrument is null) return null;

        return await ComputeAsync(instrument.Ticker, suggestion.ForDate, ct);
    }

    /// <summary>
    /// Fetches bars from <paramref name="forDate"/> onwards and returns the 5-bar forward return
    /// as a percentage, or <see langword="null"/> if the window is incomplete.
    /// </summary>
    public async Task<decimal?> ComputeAsync(string ticker, DateOnly forDate, CancellationToken ct)
    {
        var bars = await barRepo.ListAsync(new PriceBarsSinceSpec(ticker, forDate), ct);
        if (bars.Count < 1) return null;

        var window = bars.Take(ForwardBars + 1).ToArray();
        if (window.Length < ForwardBars + 1) return null;

        var closeAt = window[0].Close;
        if (closeAt == 0m) return null;

        var fwdBar = window[ForwardBars];
        return (fwdBar.Close - closeAt) / closeAt * 100m;
    }
}
