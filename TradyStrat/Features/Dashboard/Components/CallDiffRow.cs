using TradyStrat.Features.AiSuggestion.CallDiff;

namespace TradyStrat.Features.Dashboard.Components;

/// <summary>One row of the today's-call diff list.</summary>
/// <param name="Kind">"changed", "added", or "removed" — drives the side glyph.</param>
/// <param name="Indicator">e.g. "200-SMA" / "RSI(14)" / "Ichimoku" / "Zone".</param>
/// <param name="Ticker">e.g. "CON3.L".</param>
/// <param name="Detail">Free-form delta text, e.g. "Below 200-SMA → Above" — empty for added/removed rows.</param>
public sealed record CallDiffRow(string Kind, string Indicator, string Ticker, string Detail);

public static class CallDiffRowProjector
{
    public static IReadOnlyList<CallDiffRow> Project(CallDiff diff)
    {
        var rows = new List<CallDiffRow>(
            diff.ChangedCitations.Count + diff.AddedCitationKeys.Count + diff.RemovedCitationKeys.Count);

        foreach (var c in diff.ChangedCitations)
        {
            var (ind, tk) = SplitKey(c.Key);
            rows.Add(new CallDiffRow("changed", ind, tk, $"{c.PriorValue} → {c.NewValue}"));
        }

        foreach (var key in diff.AddedCitationKeys)
        {
            var (ind, tk) = SplitKey(key);
            rows.Add(new CallDiffRow("added", ind, tk, ""));
        }

        foreach (var key in diff.RemovedCitationKeys)
        {
            var (ind, tk) = SplitKey(key);
            rows.Add(new CallDiffRow("removed", ind, tk, ""));
        }

        return rows;
    }

    private static (string Indicator, string Ticker) SplitKey(string key)
    {
        var idx = key.IndexOf(':');
        return idx < 0 ? (key, "") : (key[..idx], key[(idx + 1)..]);
    }
}
