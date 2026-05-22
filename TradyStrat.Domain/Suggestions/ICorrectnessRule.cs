namespace TradyStrat.Domain;

/// <summary>
/// Pure-domain predicate: was this AI suggestion borne out by subsequent market behaviour?
/// The implementation defines the threshold/window. Out-of-scope future variants:
/// ATR-scaled, regime-aware. Today: <see cref="FixedThresholdCorrectness"/>.
/// </summary>
public interface ICorrectnessRule
{
    bool Evaluate(SuggestionAction action, decimal fwdReturnPct);
}
