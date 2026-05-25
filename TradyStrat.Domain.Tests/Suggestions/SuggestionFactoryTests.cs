using Shouldly;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Domain.Suggestions.Events;
using Xunit;

namespace TradyStrat.Domain.Tests.Suggestions;

public class SuggestionFactoryTests
{
    private static readonly DateTime _now = new(2026, 5, 22, 12, 0, 0, DateTimeKind.Utc);

    private static Suggestion ValidSuggestion() => Suggestion.From(
        instrumentId: new InstrumentId(1),
        forDate:      new DateOnly(2026, 5, 22),
        action:       SuggestionAction.Hold,
        quantityHint: Quantity.None,
        maxPriceHint: Price.None(Currency.Eur),
        conviction:   Conviction.Of(7),
        rationale:    "Sample rationale.",
        citations:    [],
        snapshot:     MarketSnapshot.Empty,
        fingerprint:  PromptFingerprint.Of("hash1", "env1", "v1"),
        thinkingText: "",
        createdAt:    _now);

    [Fact]
    public void From_assigns_zero_id_sentinel()
    {
        ValidSuggestion().Id.ShouldBe(SuggestionId.New());
    }

    [Fact]
    public void From_preserves_all_fields()
    {
        var s = ValidSuggestion();
        s.InstrumentId.ShouldBe(new InstrumentId(1));
        s.ForDate.ShouldBe(new DateOnly(2026, 5, 22));
        s.Action.ShouldBe(SuggestionAction.Hold);
        s.QuantityHint.IsSpecified.ShouldBeFalse();
        s.MaxPriceHint.IsEmpty.ShouldBeTrue();
        s.Conviction.Value.ShouldBe(7);
        s.Rationale.ShouldBe("Sample rationale.");
        s.Citations.ShouldBeEmpty();
        s.Snapshot.IsEmpty.ShouldBeTrue();
        s.Fingerprint.PromptHash.ShouldBe("hash1");
        s.ThinkingText.ShouldBe("");
        s.CreatedAt.ShouldBe(_now);
    }

    [Fact]
    public void From_rejects_empty_rationale()
    {
        Should.Throw<ArgumentException>(() => Suggestion.From(
            instrumentId: new InstrumentId(1),
            forDate:      new DateOnly(2026, 5, 22),
            action:       SuggestionAction.Hold,
            quantityHint: Quantity.None,
            maxPriceHint: Price.None(Currency.Eur),
            conviction:   Conviction.Of(5),
            rationale:    "",
            citations:    [],
            snapshot:     MarketSnapshot.Empty,
            fingerprint:  PromptFingerprint.Of("h", "", ""),
            thinkingText: "",
            createdAt:    _now));
    }

    [Fact]
    public void OrderValue_is_None_when_quantity_or_price_absent()
    {
        var s = ValidSuggestion();
        s.OrderValue.IsEmpty.ShouldBeTrue();
        s.OrderValue.Currency.ShouldBe(Currency.Eur);
    }

    [Fact]
    public void OrderValue_multiplies_quantity_and_price_when_both_specified()
    {
        var s = Suggestion.From(
            instrumentId: new InstrumentId(1),
            forDate:      new DateOnly(2026, 5, 22),
            action:       SuggestionAction.Acquire,
            quantityHint: Quantity.Of(10m),
            maxPriceHint: Price.Of(Money.Of(4m, Currency.Eur)),
            conviction:   Conviction.Of(8),
            rationale:    "Buy on dip.",
            citations:    [new Citation("rsi oversold", "rsi", "CON3.L", "28.5")],
            snapshot:     MarketSnapshot.Empty,
            fingerprint:  PromptFingerprint.Of("h", "", ""),
            thinkingText: "",
            createdAt:    _now);

        s.OrderValue.ShouldBe(Money.Of(40m, Currency.Eur));
    }

    [Fact]
    public void Citations_are_preserved_and_immutable_to_caller()
    {
        var input = new List<Citation> { new("c1", "rsi", "X", "v1"), new("c2", "sma", "X", "v2") };
        var s = Suggestion.From(
            instrumentId: new InstrumentId(1),
            forDate:      new DateOnly(2026, 5, 22),
            action:       SuggestionAction.Hold,
            quantityHint: Quantity.None,
            maxPriceHint: Price.None(Currency.Eur),
            conviction:   Conviction.Of(7),
            rationale:    "x",
            citations:    input,
            snapshot:     MarketSnapshot.Empty,
            fingerprint:  PromptFingerprint.Of("h", "", ""),
            thinkingText: "",
            createdAt:    _now);

        s.Citations.Count.ShouldBe(2);
        s.Citations[0].Claim.ShouldBe("c1");
    }

    [Fact]
    public void From_raises_SuggestionCreated()
    {
        var createdAt = new DateTime(2026, 5, 25, 9, 0, 0, DateTimeKind.Utc);
        var s = Suggestion.From(
            instrumentId:   new InstrumentId(7),
            forDate:        new DateOnly(2026, 5, 25),
            action:         SuggestionAction.Acquire,
            quantityHint:   Quantity.None,
            maxPriceHint:   Price.None(Currency.Eur),
            conviction:     Conviction.Of(3),
            rationale:      "test",
            citations:      [],
            snapshot:       MarketSnapshot.Empty,
            fingerprint:    PromptFingerprint.Of("h", "", ""),
            thinkingText:   "",
            createdAt:      createdAt);

        var evt = s.DomainEvents.OfType<SuggestionCreated>().ShouldHaveSingleItem();
        evt.InstrumentId.ShouldBe(new InstrumentId(7));
        evt.ForDate.ShouldBe(new DateOnly(2026, 5, 25));
        evt.Action.ShouldBe(SuggestionAction.Acquire);
        evt.OccurredAt.ShouldBe(createdAt);
    }
}
