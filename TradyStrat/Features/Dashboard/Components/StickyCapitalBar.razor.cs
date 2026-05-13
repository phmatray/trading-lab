using Microsoft.AspNetCore.Components;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class StickyCapitalBar : ComponentBase
{
    [Parameter, EditorRequired] public decimal CurrentValueEur { get; set; }
    [Parameter, EditorRequired] public decimal TargetEur { get; set; }
    [Parameter, EditorRequired] public decimal ProgressPct { get; set; }
    [Parameter] public SuggestionAction? Action { get; set; }

    private string Verb => SuggestionActionDisplay.Verb(Action);
    private string Stem => SuggestionActionDisplay.Stem(Action);
}
