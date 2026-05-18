namespace TradyStrat.Domain;

/// <summary>
/// Acquire iff fwd_return &gt; +threshold.
/// Trim    iff fwd_return &lt; −threshold.
/// Hold/Wait iff |fwd_return| &lt; threshold.
/// Spec §4.3.
/// </summary>
public sealed class FixedThresholdCorrectness(decimal thresholdPct) : ICorrectnessRule
{
    public bool Evaluate(SuggestionAction action, decimal fwdReturnPct) => action switch
    {
        SuggestionAction.Acquire => fwdReturnPct >  thresholdPct,
        SuggestionAction.Trim    => fwdReturnPct < -thresholdPct,
        SuggestionAction.Hold    => Math.Abs(fwdReturnPct) < thresholdPct,
        SuggestionAction.Wait    => Math.Abs(fwdReturnPct) < thresholdPct,
        _                        => false,
    };
}
