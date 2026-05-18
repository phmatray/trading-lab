namespace TradyStrat.Domain;

/// <summary>Single source of truth for how a SuggestionAction is presented:
/// the display verb ("Hold") and the lowercase CSS stem ("hold") used by
/// [data-verb] selectors and --verb-color-* tokens.</summary>
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
