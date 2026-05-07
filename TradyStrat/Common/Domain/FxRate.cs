namespace TradyStrat.Common.Domain;

public sealed record FxRate
{
    public required int Id { get; init; }
    public required DateOnly Date { get; init; }
    public required string Pair { get; init; }
    public required decimal UsdPerEur { get; init; }
    public required DateTime FetchedAt { get; init; }

    public decimal EurPerUsd => 1m / UsdPerEur;
}
