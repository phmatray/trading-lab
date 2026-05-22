using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed class Instrument
{
    public InstrumentId   Id        { get; private set; }
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
    {
        Id        = id;
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
    /// until Confirm(clock) runs.
    /// </summary>
    public static Instrument Probed(
        string ticker, string name,
        Currency currency, Exchange exchange, TimezoneId timezoneId,
        InstrumentKind kind)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Ticker is required.", nameof(ticker));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        return new Instrument(
            id:       InstrumentId.New(),
            ticker:   ticker.Trim().ToUpperInvariant(),
            name:     name.Trim(),
            currency: currency,
            exchange: exchange,
            timezone: timezoneId,
            kind:     kind,
            addedAt:  default);
    }

    /// <summary>
    /// Rehydration factory used by EF mapping to recreate the AR from
    /// persisted state. Skips Confirm because AddedAt is already known.
    /// </summary>
    public static Instrument Existing(
        InstrumentId id, string ticker, string name,
        Currency currency, Exchange exchange, TimezoneId timezoneId,
        InstrumentKind kind, DateTime addedAt)
        => new(id, ticker, name, currency, exchange, timezoneId, kind, addedAt);

    /// <summary>
    /// Promote a Probed instrument to persistable by stamping AddedAt from the
    /// clock. Throws if already confirmed.
    /// </summary>
    public void Confirm(IClock clock)
    {
        if (AddedAt != default)
            throw new InvalidOperationException(
                $"Instrument '{Ticker}' is already confirmed (AddedAt = {AddedAt:O}).");
        AddedAt = clock.UtcNow();
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Name must not be empty.", nameof(newName));
        Name = newName.Trim();
    }
}
