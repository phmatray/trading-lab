using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Infrastructure.Exceptions;
using System.Text.Json;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Application.Settings.Config;
using Microsoft.Extensions.AI;
using TradyStrat.Domain;

namespace TradyStrat.Infrastructure.AiSuggestion;

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
                    PromptHash        = snapshot.PromptHash,
                    EnvelopeHash      = snapshot.EnvelopeHash,
                    PromptVersionHash = snapshot.PromptVersionHash,
                    // ThinkingText set after the response is read; see below.
                    CreatedAt    = clock.UtcNow(),
                };
                return "ok";
            },
            name: ToolName,
            description: "Submit your structured trading suggestion with cited reasoning.");

        var options = new ChatOptions
        {
            Tools           = [submit],
            // Cannot force a specific tool when extended thinking is enabled —
            // Anthropic rejects the combination ("Thinking may not be enabled
            // when tool_choice forces tool use."). The system prompt's
            // "Always invoke the submit_suggestion tool exactly once." line
            // is the contract that compels the call. If the model ever doesn't,
            // AskAsync throws AnthropicCallFailedException ("Model did not
            // invoke submit_suggestion") — same as before.
            ToolMode        = ChatToolMode.Auto,
            ModelId         = ai.Model,
            MaxOutputTokens = ai.MaxTokens,
        };

        // Envelope: stable across instruments on the same day; flagged for the
        // cache decorator. Spec §5.1.
        var envelopeJson = JsonSerializer.Serialize(new
        {
            today = snapshot.Today,
            goal = snapshot.Goal,
            portfolio = snapshot.Portfolio,
            tickers = snapshot.Tickers,
            recent_trades = snapshot.RecentTrades,
            usd_per_eur = snapshot.UsdPerEur,
            markets = snapshot.Markets,
        }, JsonOpts.Strict);

        var focusJson = JsonSerializer.Serialize(new
        {
            instrument_id = snapshot.InstrumentId,
            recent_suggestions = snapshot.RecentSuggestions,
        }, JsonOpts.Strict);

        var envelope = new TextContent(envelopeJson)
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [CacheControlChatClient.CacheBreakpointKey] = true,
            },
        };
        var focus = new TextContent(focusJson);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User,   [envelope, focus]),
        };

        ChatResponse response;
        try
        {
            response = await chat.GetResponseAsync(messages, options, ct);
        }
        catch (AnthropicCallFailedException) { throw; }
        catch (Exception ex)
        {
            LogCallFailed(log, ex);
            throw new AnthropicCallFailedException("Anthropic call failed.", ex);
        }

        if (captured is null)
            throw new AnthropicCallFailedException("Model did not invoke submit_suggestion.");

        // Read harvested thinking text mirrored by ThinkingHarvestChatClient.
        if (response.AdditionalProperties is { } props
            && props.TryGetValue(ThinkingHarvestChatClient.ThinkingTextKey, out var t)
            && t is string s
            && !string.IsNullOrEmpty(s))
        {
            captured = captured with { ThinkingText = s };
        }

        return captured;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Anthropic call failed")]
    private static partial void LogCallFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI cited unknown market slug {Slug}; dropped")]
    private static partial void LogUnknownMarketCitation(ILogger logger, string slug);
}
