using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Features.PredictionMarkets;

namespace TradyStrat.Features.Dashboard.Components;

public partial class MarketsRail : ComponentBase
{
    [Parameter, EditorRequired]
    public MarketSnapshot Snapshot { get; set; } = MarketSnapshot.Empty;

    private Dictionary<string, MarketCitation> _bySlug = new();

    protected override void OnParametersSet()
        => _bySlug = Snapshot.Cited.ToDictionary(c => c.Slug);   // hygiene already deduped server-side

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");
}
