using Ardalis.Specification;
using TradyStrat.Application.AiSuggestion.Specifications;
using TradyStrat.Application.Fx;
using TradyStrat.Application.PriceFeed.Specifications;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.Trades.Specifications;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

/// <summary>
/// Closed-loop outcome-feedback section (spec §4.2).
/// For each of the last 30 suggestions on this instrument, emits a row carrying
/// the original action/conviction/rationale-headline plus computed forward-window
/// fields: 5-bar forward return, was-correct (via <see cref="ICorrectnessRule"/>),
/// is-forward-window-complete sentinel, and optional net trade cash flow in EUR.
/// </summary>
public sealed class RecentSuggestionsSection(
    IReadRepositoryBase<Suggestion> suggestionRepo,
    IReadRepositoryBase<PriceBar> barRepo,
    IReadRepositoryBase<Trade> tradeRepo,
    ListInstrumentsUseCase listInstruments,
    FxConverter fx,
    ICorrectnessRule correctness) : ISnapshotSectionProvider
{
    private const int LookbackCount = 30;
    private const int ForwardBars   = 5;
    private const int HeadlineMax   = 80;

    public int Order => 60;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        var raw = await suggestionRepo.ListAsync(
            new RecentSuggestionsForInstrumentSpec(instrumentId, asOf, LookbackCount), ct);
        if (raw.Count == 0) return;

        var ordered = raw.OrderBy(s => s.ForDate).ToArray();   // chronological for the JSON

        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var instrument = instruments.SingleOrDefault(i => i.Id == instrumentId);
        if (instrument is null) return;
        var ticker = instrument.Ticker;
        var currency = instrument.Currency;

        foreach (var s in ordered)
        {
            var bars = await barRepo.ListAsync(
                new PriceBarsSinceSpec(ticker, s.ForDate), ct);

            if (bars.Count < 1) continue;                       // closeAt missing — skip
            // PriceBarsSinceSpec is >= s.ForDate; take the first ForwardBars+1 bars.
            var window = bars.Take(ForwardBars + 1).ToArray();
            var closeAt = window[0].Close;

            if (window.Length < ForwardBars + 1)
            {
                // Forward window incomplete — emit context-only row.
                builder.RecentSuggestions.Add(BuildRow(s, fwdReturnPct: 0m, wasCorrect: false,
                    isComplete: false, netFlowEur: null));
                continue;
            }

            var fwdBar = window[ForwardBars];
            var fwdReturnPct = closeAt == 0m ? 0m : (fwdBar.Close - closeAt) / closeAt * 100m;
            var wasCorrect = correctness.Evaluate(s.Action, fwdReturnPct);
            var netFlowEur = await ComputeNetFlowEurAsync(instrumentId, s.ForDate, fwdBar.Date, currency, ct);

            builder.RecentSuggestions.Add(BuildRow(s, fwdReturnPct, wasCorrect,
                isComplete: true, netFlowEur));
        }
    }

    private async Task<decimal?> ComputeNetFlowEurAsync(
        int instrumentId, DateOnly after, DateOnly through, string currency, CancellationToken ct)
    {
        var trades = await tradeRepo.ListAsync(
            new TradesOnInstrumentInWindowSpec(instrumentId, after, through), ct);
        if (trades.Count == 0) return null;

        decimal sum = 0m;
        foreach (var t in trades)
        {
            var sign = t.Side == TradeSide.Buy ? -1m : 1m;
            var notional = sign * t.Quantity * t.PricePerShare;
            var notionalEur = string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase)
                ? notional
                : await fx.ToEurAsync(notional, currency, t.ExecutedOn, ct);
            sum += notionalEur;
        }
        return sum;
    }

    private static PastSuggestionRow BuildRow(
        Suggestion s, decimal fwdReturnPct, bool wasCorrect, bool isComplete, decimal? netFlowEur)
        => new(
            Date:                    s.ForDate,
            Action:                  s.Action,
            Conviction:              s.Conviction,
            FwdReturnPct:            fwdReturnPct,
            WasCorrect:              wasCorrect,
            IsForwardWindowComplete: isComplete,
            NetTradeFlowEur:         netFlowEur,
            RationaleHeadline:       Headline(s.Rationale));

    private static string Headline(string rationale)
    {
        if (string.IsNullOrEmpty(rationale)) return string.Empty;
        var slice = rationale.Length <= HeadlineMax
            ? rationale
            : rationale[..HeadlineMax];
        if (slice.Length == HeadlineMax)
        {
            var lastSpace = slice.LastIndexOf(' ');
            if (lastSpace > 0) slice = slice[..lastSpace];
        }
        return slice.TrimEnd();
    }
}
