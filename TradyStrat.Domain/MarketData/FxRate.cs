namespace TradyStrat.Domain;

public sealed record FxRate
{
    public required int Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string Base { get; init; }     // ISO 4217, e.g. "EUR"
    public required string Quote { get; init; }    // ISO 4217, e.g. "USD"
    public required decimal Rate { get; init; }    // Quote per 1 Base
    public required DateTime FetchedAt { get; init; }
}
