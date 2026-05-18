using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Common.Exceptions;
using System.Text.Json;
using TradyStrat.Application.PredictionMarkets;
using TradyStrat.Application.Settings.Config;
using Microsoft.Extensions.AI;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Features.AiSuggestion;

public sealed partial class SuggestionService(
    IChatClient chat, IClock clock, ILogger<SuggestionService> log, ISettingsReader settings) : IAiClient
{
    private const string ToolName = "submit_suggestion";
    private const string SystemPrompt = """
        You are a disciplined trading assistant for a personal accumulation strategy
        on CON3 (a 3x leveraged Coinbase ETP). You see snapshots once per day.
        Cite which indicators support each part of your suggestion.
        Be conservative: when signals conflict, say Hold.
        Always invoke the submit_suggestion tool exactly once.

        You may also cite Polymarket markets you weighed.
        Each market_citations[].slug MUST appear in the snapshot's markets[].
        Cite each market at most once.
        Cite a market only when you actually weighted it; not every market needs a citation.
        """;

    public async Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
    {
        var ai = await settings.AnthropicAsync(ct);
        Suggestion? captured = null;

        var submit = AIFunctionFactory.Create(
            (SuggestionAction action, decimal? quantity_hint, decimal? max_price_hint,
             int conviction, string rationale,
             IReadOnlyList<Citation>? citations,
             IReadOnlyList<MarketCitation>? market_citations) =>
            {
                var citationList = citations ?? [];

                var validSlugs = snapshot.Markets.Select(m => m.Slug).ToHashSet();
                var cleanedMarketCitations = (market_citations ?? [])
                    .Where(c =>
                    {
                        if (validSlugs.Contains(c.Slug)) return true;
                        LogUnknownMarketCitation(log, c.Slug);
                        return false;
                    })
                    .GroupBy(c => c.Slug)
                    .Select(g => g.First())
                    .ToList();

                var marketJson = snapshot.Markets.Count == 0
                    ? null
                    : JsonSerializer.Serialize(
                        new MarketSnapshot(snapshot.Markets, cleanedMarketCitations),
                        JsonOpts.Strict);

                captured = new Suggestion
                {
                    Id           = 0,
                    InstrumentId = snapshot.InstrumentId,        // NEW (Phase 2)
                    ForDate      = snapshot.Today,
                    Action       = action,
                    QuantityHint = quantity_hint,
                    MaxPriceHint = max_price_hint,
                    Conviction   = conviction,
                    Rationale    = rationale,
                    CitationsJson = JsonSerializer.Serialize(citationList, JsonOpts.Strict),
                    MarketSnapshotJson = marketJson,
                    PromptHash   = snapshot.PromptHash,
                    CreatedAt    = clock.UtcNow(),
                };
                return "ok";
            },
            name: ToolName,
            description: "Submit your structured trading suggestion with cited reasoning.");

        var options = new ChatOptions
        {
            Tools           = [submit],
            ToolMode        = ChatToolMode.RequireSpecific(ToolName),
            ModelId         = ai.Model,
            MaxOutputTokens = ai.MaxTokens,
        };

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User,   JsonSerializer.Serialize(snapshot, JsonOpts.Strict)),
        };

        try
        {
            await chat.GetResponseAsync(messages, options, ct);
        }
        catch (AnthropicCallFailedException) { throw; }
        catch (Exception ex)
        {
            LogCallFailed(log, ex);
            throw new AnthropicCallFailedException("Anthropic call failed.", ex);
        }

        return captured
            ?? throw new AnthropicCallFailedException(
                "Model did not invoke submit_suggestion.");
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Anthropic call failed")]
    private static partial void LogCallFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI cited unknown market slug {Slug}; dropped")]
    private static partial void LogUnknownMarketCitation(ILogger logger, string slug);
}
