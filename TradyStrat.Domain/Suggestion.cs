using System.Text.Json;
using System.Text.Json.Serialization;

namespace TradyStrat.Domain;

public sealed record Suggestion
{
    // Citations are stored snake_case (matches the AI tool-call shape), so deserializing
    // back to PascalCase records needs the same naming policy on the way out.
    private static readonly JsonSerializerOptions CitationOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };

    public required int Id { get; init; }
    public required int InstrumentId { get; init; }   // NEW (Phase 2)
    public required DateOnly ForDate { get; init; }
    public required SuggestionAction Action { get; init; }
    public decimal? QuantityHint { get; init; }
    public decimal? MaxPriceHint { get; init; }
    public required int Conviction { get; init; }
    public required string Rationale { get; init; }
    public required string CitationsJson { get; init; }
    public string? MarketSnapshotJson { get; init; }
    public required string PromptHash { get; init; }
    public required DateTime CreatedAt { get; init; }

    public decimal? OrderValueEur =>
        QuantityHint is { } q && MaxPriceHint is { } p ? q * p : null;

    public IReadOnlyList<Citation> Citations =>
        JsonSerializer.Deserialize<List<Citation>>(CitationsJson, CitationOpts) ?? [];
}
