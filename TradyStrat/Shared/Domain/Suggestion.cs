using System.Text.Json;

namespace TradyStrat.Shared.Domain;

public sealed record Suggestion
{
    public required int Id { get; init; }
    public required DateOnly ForDate { get; init; }
    public required SuggestionAction Action { get; init; }
    public decimal? QuantityHint { get; init; }
    public decimal? MaxPriceHint { get; init; }
    public required int Conviction { get; init; }
    public required string Rationale { get; init; }
    public required string CitationsJson { get; init; }
    public required string PromptHash { get; init; }
    public required DateTime CreatedAt { get; init; }

    public decimal? OrderValueEur =>
        QuantityHint is decimal q && MaxPriceHint is decimal p ? q * p : null;

    public IReadOnlyList<Citation> Citations =>
        JsonSerializer.Deserialize<List<Citation>>(CitationsJson) ?? [];
}
