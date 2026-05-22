using Shouldly;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain.Suggestions.Services;
using Xunit;

namespace TradyStrat.Domain.Tests.Suggestions;

public class SuggestionGateTests
{
    private static readonly DateTime _now = new(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);

    private static Suggestion Existing(PromptFingerprint fp) => Suggestion.From(
        instrumentId: new InstrumentId(1),
        forDate:      new DateOnly(2026, 5, 22),
        action:       SuggestionAction.Hold,
        quantityHint: Quantity.None,
        maxPriceHint: Price.None(Currency.Eur),
        conviction:   Conviction.Of(5),
        rationale:    "ok",
        citations:    [],
        snapshot:     MarketSnapshot.Empty,
        fingerprint:  fp,
        thinkingText: "",
        createdAt:    _now);

    [Fact]
    public void Decide_returns_Fetch_when_no_existing_suggestion()
    {
        var decision = SuggestionGate.Decide(
            existing: null,
            candidateFingerprint: PromptFingerprint.Of("hashA", "envA", "v1"));
        decision.ShouldBeOfType<GateDecision.Fetch>();
    }

    [Fact]
    public void Decide_returns_Reuse_when_fingerprint_matches_existing()
    {
        var fp = PromptFingerprint.Of("hashA", "envA", "v1");
        var existing = Existing(fp);
        var decision = SuggestionGate.Decide(
            existing: existing,
            candidateFingerprint: PromptFingerprint.Of("hashA", "envA", "v1"));
        decision.ShouldBeOfType<GateDecision.Reuse>();
        ((GateDecision.Reuse)decision).Existing.ShouldBeSameAs(existing);
    }

    [Fact]
    public void Decide_returns_Fetch_when_prompt_hash_differs()
    {
        var existing = Existing(PromptFingerprint.Of("hashA", "envA", "v1"));
        var decision = SuggestionGate.Decide(
            existing: existing,
            candidateFingerprint: PromptFingerprint.Of("hashB", "envA", "v1"));
        decision.ShouldBeOfType<GateDecision.Fetch>();
    }

    [Fact]
    public void Decide_returns_Fetch_when_envelope_hash_differs()
    {
        var existing = Existing(PromptFingerprint.Of("hashA", "envA", "v1"));
        var decision = SuggestionGate.Decide(
            existing: existing,
            candidateFingerprint: PromptFingerprint.Of("hashA", "envB", "v1"));
        decision.ShouldBeOfType<GateDecision.Fetch>();
    }

    [Fact]
    public void Decide_returns_Fetch_when_prompt_version_differs()
    {
        var existing = Existing(PromptFingerprint.Of("hashA", "envA", "v1"));
        var decision = SuggestionGate.Decide(
            existing: existing,
            candidateFingerprint: PromptFingerprint.Of("hashA", "envA", "v2"));
        decision.ShouldBeOfType<GateDecision.Fetch>();
    }
}
