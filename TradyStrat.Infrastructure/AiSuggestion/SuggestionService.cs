using System.Text.Json;
using Microsoft.Extensions.AI;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.Settings;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Infrastructure.Exceptions;

namespace TradyStrat.Infrastructure.AiSuggestion;

public sealed partial class SuggestionService(
    IChatClient chat, ILogger<SuggestionService> log, IAnthropicSettingsRepository anthropic) : IAiClient
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

    public async Task<AiResponse> AskAsync(AiSnapshot snapshot, CancellationToken ct)
    {
        var ai = await anthropic.GetAsync(ct);
        AiResponse? captured = null;

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

                var marketSnapshot = snapshot.Markets.Count == 0
                    ? MarketSnapshot.Empty
                    : new MarketSnapshot(snapshot.Markets, cleanedMarketCitations);

                captured = new AiResponse(
                    Action:        action,
                    QuantityHint:  quantity_hint,
                    MaxPriceHint:  max_price_hint,
                    Conviction:    conviction,
                    Rationale:     rationale,
                    Citations:     citationList,
                    Snapshot:      marketSnapshot,
                    ThinkingText:  "");
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
            // is the contract that compels the call.
            ToolMode        = ChatToolMode.Auto,
            ModelId         = ai.Model.Value,
            MaxOutputTokens = ai.MaxTokens.Value,
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
