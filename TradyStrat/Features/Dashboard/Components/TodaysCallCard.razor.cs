using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Features.Dashboard.Components;

public partial class TodaysCallCard : ComponentBase
{
    [Parameter, EditorRequired] public Suggestion Sug { get; set; } = null!;
    [Parameter] public DateOnly Today { get; set; }
    [Parameter] public EventCallback OnLogTrade { get; set; }
    [Parameter] public EventCallback OnRerun { get; set; }

    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    private string Verb => Sug.Action switch
    {
        SuggestionAction.Acquire => "Acquire.",
        SuggestionAction.Hold    => "Hold.",
        SuggestionAction.Trim    => "Trim.",
        SuggestionAction.Wait    => "Wait.",
        _ => "—"
    };
}
