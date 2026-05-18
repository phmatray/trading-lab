namespace TradyStrat.Domain;

public sealed record InstrumentMetadata(
    string Ticker,
    string Name,
    string Currency,
    string Exchange,
    string TimezoneId);
