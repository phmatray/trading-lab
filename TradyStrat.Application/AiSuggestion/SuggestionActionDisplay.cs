using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion;

/// <summary>Single source of truth for how a SuggestionAction is presented:
/// the display verb ("Hold") and the lowercase stem ("hold"). Stem values
/// are also keyed by Web CSS ([data-verb] selectors, --verb-color-* tokens)
/// so they must stay stable.</summary>
public static class SuggestionActionDisplay
{
    public static string Verb(SuggestionAction? action) => action switch
    {
        SuggestionAction.Acquire => "Acquire",
        SuggestionAction.Hold    => "Hold",
        SuggestionAction.Trim    => "Trim",
        SuggestionAction.Wait    => "Wait",
        _ => "—",
    };

    public static string Stem(SuggestionAction? action) => action switch
    {
        SuggestionAction.Acquire => "acquire",
        SuggestionAction.Hold    => "hold",
        SuggestionAction.Trim    => "trim",
        SuggestionAction.Wait    => "wait",
        _ => "none",
    };
}
