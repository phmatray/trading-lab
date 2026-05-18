using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using TradyStrat.Application.PredictionMarkets;

namespace TradyStrat.Features.Dashboard.Components;

public partial class MarketsRail : ComponentBase
{
    [Parameter, EditorRequired]
    public MarketSnapshot Snapshot { get; set; } = MarketSnapshot.Empty;

    private Dictionary<string, MarketCitation> _bySlug = new();

    protected override void OnParametersSet()
        => _bySlug = Snapshot.Cited.ToDictionary(c => c.Slug);   // hygiene already deduped server-side

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private static readonly Regex LadderRe = new(
        @"^Will\s+(\d+)(\s+or\s+more)?\s+Fed\s+rate\s+cuts?\s+happen\s+in\s+2026\??$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    protected readonly record struct LadderBucket(int Cuts, bool OrMore, decimal Probability);

    protected sealed record GroupedMarkets(
        IReadOnlyList<PredictionMarket> Cited,
        IReadOnlyList<LadderBucket> Ladder,
        IReadOnlyList<PredictionMarket> Other);

    protected GroupedMarkets Group()
    {
        var cited = new List<PredictionMarket>();
        var ladder = new List<LadderBucket>();
        var other = new List<PredictionMarket>();
        foreach (var m in Snapshot.Markets)
        {
            if (_bySlug.ContainsKey(m.Slug)) { cited.Add(m); continue; }
            var match = LadderRe.Match(m.Question);
            if (match.Success)
            {
                ladder.Add(new LadderBucket(
                    int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture),
                    match.Groups[2].Success,
                    m.Probability));
                continue;
            }
            other.Add(m);
        }
        ladder.Sort((a, b) => a.Cuts.CompareTo(b.Cuts));
        return new GroupedMarkets(cited, ladder, other);
    }

    // Show buckets >= 0.5% plus always the last (highest) bucket — by index.
    protected static IEnumerable<int> VisibleBucketIndices(IReadOnlyList<LadderBucket> all)
    {
        for (var i = 0; i < all.Count; i++)
            if (all[i].Probability >= 0.005m || i == all.Count - 1) yield return i;
    }
}
