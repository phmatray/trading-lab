using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Polymarket;

public sealed record SearchQueries
{
    public IReadOnlyList<string> Values { get; }

    private SearchQueries(IReadOnlyList<string> values) => Values = values;

    public static SearchQueries Of(IEnumerable<string> raw)
    {
        var normalized = new List<string>();
        foreach (var q in raw ?? Enumerable.Empty<string>())
        {
            var t = (q ?? "").Trim().ToLowerInvariant();
            if (t.Length == 0)
                throw new SettingValidationException("Search queries cannot contain blank entries.");
            normalized.Add(t);
        }
        if (normalized.Count == 0)
            throw new SettingValidationException("Search queries must contain at least one entry.");
        return new SearchQueries(normalized);
    }
}
