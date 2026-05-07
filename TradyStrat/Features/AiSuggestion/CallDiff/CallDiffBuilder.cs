using System.Globalization;
using System.Text;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.AiSuggestion.CallDiff;

public sealed class CallDiffBuilder
{
    private Suggestion? _today;
    private Suggestion? _prior;

    public CallDiffBuilder WithToday(Suggestion today) { _today = today; return this; }
    public CallDiffBuilder WithPrior(Suggestion? prior) { _prior = prior; return this; }

    public CallDiff Build()
    {
        if (_today is null) throw new InvalidOperationException("WithToday(...) is required.");
        if (_prior is null) return CallDiff.None;

        var todayCits = _today.Citations.ToDictionary(Key, c => c.Value);
        var priorCits = _prior.Citations.ToDictionary(Key, c => c.Value);

        var added   = todayCits.Keys.Except(priorCits.Keys).OrderBy(k => k).ToList();
        var removed = priorCits.Keys.Except(todayCits.Keys).OrderBy(k => k).ToList();
        var changed = todayCits
            .Where(kv => priorCits.TryGetValue(kv.Key, out var prior) && prior != kv.Value)
            .Select(kv => new CitationChange(kv.Key, priorCits[kv.Key], kv.Value))
            .OrderBy(c => c.Key)
            .ToList();

        var actionChanged   = _today.Action != _prior.Action;
        var convictionDelta = _today.Conviction - _prior.Conviction;

        return new CallDiff(
            ActionChanged: actionChanged,
            PriorAction: _prior.Action,
            ConvictionDelta: convictionDelta,
            AddedCitationKeys: added,
            RemovedCitationKeys: removed,
            ChangedCitations: changed,
            SummaryParagraph: BuildSummary(_today, _prior, added, removed, changed));
    }

    private static string Key(Citation c) => $"{c.Indicator}:{c.Ticker}";

    private static string BuildSummary(
        Suggestion today, Suggestion prior,
        IReadOnlyList<string> added, IReadOnlyList<string> removed,
        IReadOnlyList<CitationChange> changed)
    {
        var ic = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();
        sb.Append(today.Action == prior.Action
            ? string.Create(ic, $"{today.Action} unchanged.")
            : string.Create(ic, $"{prior.Action} → {today.Action}."));

        var dc = today.Conviction - prior.Conviction;
        if (dc != 0)
            sb.Append(string.Create(ic, $" Conviction {today.Conviction} ({(dc > 0 ? "+" : "")}{dc})."));

        var noteworthy = new List<string>();
        foreach (var ch in changed) noteworthy.Add(string.Create(ic, $"{ch.Key} {ch.PriorValue} → {ch.NewValue}"));
        foreach (var k in added)    noteworthy.Add(string.Create(ic, $"{k} added"));
        foreach (var k in removed)  noteworthy.Add(string.Create(ic, $"{k} dropped"));
        if (noteworthy.Count > 0)
            sb.Append(' ').Append(string.Join(" · ", noteworthy)).Append('.');

        return sb.ToString();
    }
}
