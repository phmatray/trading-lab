namespace TradyStrat.Cli.Mcp.Dto;

public sealed record SuggestionPage(
    string Instrument, DateOnly From, DateOnly To, int Count,
    IReadOnlyList<SuggestionRow> Items);

public sealed record SuggestionRow(
    DateOnly Date, string Action, int Conviction,
    string? EnvelopeHash, string? PromptVersionHash,
    string Reasoning,
    decimal? ForwardReturnPct,
    bool? Correct);
