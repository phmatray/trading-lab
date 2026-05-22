namespace TradyStrat.Domain.Suggestions;

public abstract record GateDecision
{
    public sealed record Fetch : GateDecision;
    public sealed record Reuse(Suggestion Existing) : GateDecision;

    private GateDecision() { }
}
