using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Suggestions.Events;
using TradyStrat.Domain.Suggestions.Services;

namespace TradyStrat.Domain.Suggestions;

public sealed class Suggestion : AggregateRoot<SuggestionId>
{
    public InstrumentId      InstrumentId  { get; private set; }
    public DateOnly          ForDate       { get; private set; }
    public SuggestionAction  Action        { get; private set; }
    public Quantity          QuantityHint  { get; private set; } = Quantity.None;
    public Price             MaxPriceHint  { get; private set; } = Price.None(Currency.Eur);
    public Conviction        Conviction    { get; private set; } = Conviction.Of(1);
    public string            Rationale     { get; private set; } = "";
    public MarketSnapshot    Snapshot      { get; private set; } = MarketSnapshot.Empty;
    public PromptFingerprint Fingerprint   { get; private set; } = PromptFingerprint.Empty;
    public string            ThinkingText  { get; private set; } = "";
    public DateTime          CreatedAt     { get; private set; }

    private readonly List<Citation> _citations = new();
    public IReadOnlyList<Citation> Citations => _citations;

    private Suggestion() { }   // EF

    private Suggestion(
        InstrumentId instrumentId, DateOnly forDate, SuggestionAction action,
        Quantity quantityHint, Price maxPriceHint, Conviction conviction,
        string rationale, IReadOnlyList<Citation> citations,
        MarketSnapshot snapshot, PromptFingerprint fingerprint, string thinkingText,
        DateTime createdAt)
        : base(SuggestionId.New())
    {
        // Id is DB-assigned via ValueGeneratedOnAdd; the zero sentinel is rewritten on insert.
        InstrumentId = instrumentId;
        ForDate      = forDate;
        Action       = action;
        QuantityHint = quantityHint;
        MaxPriceHint = maxPriceHint;
        Conviction   = conviction;
        Rationale    = rationale;
        Snapshot     = snapshot;
        Fingerprint  = fingerprint;
        ThinkingText = thinkingText;
        CreatedAt    = createdAt;
        _citations.AddRange(citations);
    }

    public static Suggestion From(
        InstrumentId instrumentId, DateOnly forDate, SuggestionAction action,
        Quantity quantityHint, Price maxPriceHint, Conviction conviction,
        string rationale, IReadOnlyList<Citation> citations,
        MarketSnapshot snapshot, PromptFingerprint fingerprint, string thinkingText,
        DateTime createdAt)
    {
        if (string.IsNullOrWhiteSpace(rationale))
            throw new ArgumentException("Rationale is required.", nameof(rationale));

        var s = new Suggestion(
            instrumentId, forDate, action, quantityHint, maxPriceHint, conviction,
            rationale, citations ?? [], snapshot, fingerprint, thinkingText ?? "",
            createdAt);
        s.Raise(new SuggestionCreated(s.Id, instrumentId, forDate, action, createdAt));
        return s;
    }

    public Money OrderValue =>
        QuantityHint.IsSpecified && !MaxPriceHint.IsEmpty
            ? MaxPriceHint * QuantityHint
            : Money.None(MaxPriceHint.Currency);

    public Correctness WasCorrect(IReadOnlyList<PriceBar> forwardBars, ICorrectnessRule rule)
    {
        if (forwardBars.Count < 2)
            return new Correctness(false, Money.Zero(Currency.Eur));

        var start = forwardBars[0].Close;
        var end   = forwardBars[^1].Close;
        var pctReturn = start == 0m ? 0m : (end - start) / start * 100m;
        var isCorrect = rule.Evaluate(Action, pctReturn);
        var deltaPerShare = end - start;
        return new Correctness(isCorrect, Money.Of(deltaPerShare, Currency.Eur));
    }

}
