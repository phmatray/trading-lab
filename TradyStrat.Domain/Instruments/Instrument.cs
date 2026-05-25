using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Instruments.Events;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed class Instrument : AggregateRoot<InstrumentId>
{
    public string         Ticker    { get; private set; } = "";
    public string         Name      { get; private set; } = "";
    public Currency       Currency  { get; private set; } = Currency.Eur;
    public Exchange       Exchange  { get; private set; } = Exchange.Of("UNKNOWN");
    public TimezoneId     Timezone  { get; private set; } = TimezoneId.Of("UTC");
    public InstrumentKind Kind      { get; private set; }
    public DateTime       AddedAt   { get; private set; }

    private Instrument() { }   // EF

    private Instrument(
        InstrumentId id, string ticker, string name,
        Currency currency, Exchange exchange, TimezoneId timezone,
        InstrumentKind kind, DateTime addedAt)
        : base(id)
    {
        Ticker    = ticker;
        Name      = name;
        Currency  = currency;
        Exchange  = exchange;
        Timezone  = timezone;
        Kind      = kind;
        AddedAt   = addedAt;
    }

    /// <summary>
    /// Candidate instrument returned from a probe (e.g. Yahoo metadata fetch)
    /// but not yet persisted. Id is the zero sentinel; AddedAt is default
    /// until Confirm(clock) runs. Raises InstrumentProbed.
    /// </summary>
    public static Instrument Probed(
        string ticker, string name,
        Currency currency, Exchange exchange, TimezoneId timezoneId,
        InstrumentKind kind,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Ticker is required.", nameof(ticker));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        var normalisedTicker = ticker.Trim().ToUpperInvariant();
        var inst = new Instrument(
            id:       InstrumentId.New(),
            ticker:   normalisedTicker,
            name:     name.Trim(),
            currency: currency,
            exchange: exchange,
            timezone: timezoneId,
            kind:     kind,
            addedAt:  default);
        inst.Raise(new InstrumentProbed(normalisedTicker, currency, exchange, now));
        return inst;
    }

    /// <summary>Rehydration factory used by EF mapping — does not raise.</summary>
    public static Instrument Existing(
        InstrumentId id, string ticker, string name,
        Currency currency, Exchange exchange, TimezoneId timezoneId,
        InstrumentKind kind, DateTime addedAt)
        => new(id, ticker, name, currency, exchange, timezoneId, kind, addedAt);

    public void Confirm(IClock clock)
    {
        if (AddedAt != default)
            throw new InvalidOperationException(
                $"Instrument '{Ticker}' is already confirmed (AddedAt = {AddedAt:O}).");
        var now = clock.UtcNow();
        AddedAt = now;
        Raise(new InstrumentConfirmed(Id, now));
    }

    public void Rename(string newName, IClock clock)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name must not be empty.", nameof(newName));
        var trimmed = newName.Trim();
        if (trimmed == Name) return;
        var oldName = Name;
        Name = trimmed;
        Raise(new InstrumentRenamed(Id, oldName, trimmed, clock.UtcNow()));
    }
}
