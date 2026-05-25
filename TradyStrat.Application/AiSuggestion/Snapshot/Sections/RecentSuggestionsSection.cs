using TradyStrat.Application.Fx;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.PriceFeed;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain.Suggestions.Services;

namespace TradyStrat.Application.AiSuggestion.Snapshot.Sections;

/// <summary>
/// Closed-loop outcome-feedback section. For each of the last 30 suggestions on
/// this instrument, emits a row carrying the original action/conviction/rationale
/// plus computed forward-window fields: 5-bar forward return, was-correct,
/// is-forward-window-complete sentinel, and optional net trade cash flow in EUR.
/// </summary>
public sealed class RecentSuggestionsSection(
    ISuggestionRepository suggestions,
    IPriceBarReadRepository barRepo,
    IPortfolioRepository portfolios,
    ListInstrumentsUseCase listInstruments,
    FxConverter fx,
    ICorrectnessRule correctness,
    ForwardReturnCalculator forwardReturn) : ISnapshotSectionProvider
{
    private const int LookbackCount = 30;
    private const int ForwardBars   = 5;
    private const int HeadlineMax   = 80;

    public int Order => 60;

    public async Task ContributeAsync(SnapshotBuilder builder, int instrumentId, DateOnly asOf, CancellationToken ct)
    {
        var iid = new InstrumentId(instrumentId);
        var raw = await suggestions.RecentForAsync(iid, asOf, LookbackCount, ct);
        if (raw.Count == 0) return;

        var ordered = raw.OrderBy(s => s.ForDate).ToArray();   // chronological for the JSON

        var instruments = await listInstruments.ExecuteAsync(Unit.Value, ct);
        var instrument = instruments.SingleOrDefault(i => i.Id == iid);
        if (instrument is null) return;
        var ticker = instrument.Ticker;
        var currency = instrument.Currency.Code;

        var portfolio = await portfolios.GetAsync(ct);

        foreach (var s in ordered)
        {
            var bars = await barRepo.ListSinceAsync(ticker, s.ForDate, ct);

            if (bars.Count < 1) continue;

            var fwdPct = await forwardReturn.ComputeAsync(ticker, s.ForDate, ct);

            if (fwdPct is null)
            {
                builder.RecentSuggestions.Add(BuildRow(s, fwdReturnPct: 0m, wasCorrect: false,
                    isComplete: false, netFlowEur: null));
                continue;
            }

            var window = bars.Take(ForwardBars + 1).ToArray();
            var fwdBar = window[ForwardBars];
            var wasCorrect = correctness.Evaluate(s.Action, fwdPct.Value);
            var netFlowEur = await ComputeNetFlowEurAsync(
                portfolio, iid, s.ForDate, fwdBar.Date, currency, ct);

            builder.RecentSuggestions.Add(BuildRow(s, fwdPct.Value, wasCorrect,
                isComplete: true, netFlowEur));
        }
    }

    private async Task<decimal?> ComputeNetFlowEurAsync(
        global::TradyStrat.Domain.Portfolio.Portfolio portfolio,
        InstrumentId instrumentId, DateOnly after, DateOnly through, string currency, CancellationToken ct)
    {
        var position = portfolio.Positions.FirstOrDefault(p => p.InstrumentId == instrumentId);
        if (position is null) return null;

        var trades = position.Trades
            .Where(t => t.ExecutedOn > after && t.ExecutedOn <= through)
            .ToList();
        if (trades.Count == 0) return null;

        decimal sum = 0m;
        foreach (var t in trades)
        {
            var sign = t.Side == TradeSide.Buy ? -1m : 1m;
            var notional = sign * t.Quantity.Value * t.PricePerShare.PerUnit.Amount;
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
            Conviction:              s.Conviction.Value,
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
