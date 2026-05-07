namespace TradyStrat.Common.Domain;

public sealed record Instrument
{
    public required int Id { get; init; }
    public required string Ticker { get; init; }
    public required string Name { get; init; }
    public required string Currency { get; init; }     // ISO 4217, e.g. "USD", "EUR"
    public required string Exchange { get; init; }     // Yahoo fullExchangeName
    public required string TimezoneId { get; init; }   // IANA, e.g. "Europe/London"
    public required InstrumentKind Kind { get; init; }
    public required DateTime AddedAt { get; init; }
}
