using System.Text.Json;
using Microsoft.Extensions.AI;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;

namespace TradyStrat.Features.AiSuggestion;

public sealed partial class SuggestionService(
    IChatClient chat, IClock clock, ILogger<SuggestionService> log) : IAiClient
{
    private const string ToolName = "submit_suggestion";
    private const string SystemPrompt = """
        You are a disciplined trading assistant for a personal accumulation strategy
        on CON3 (a 3x leveraged Coinbase ETP). You see snapshots once per day.
        Cite which indicators support each part of your suggestion.
        Be conservative: when signals conflict, say Hold.
        Always invoke the submit_suggestion tool exactly once.
        """;

    public async Task<Suggestion> AskAsync(AiSnapshot snapshot, CancellationToken ct)
    {
        Suggestion? captured = null;

        var submit = AIFunctionFactory.Create(
            (SuggestionAction action, decimal? quantity_hint, decimal? max_price_hint,
             int conviction, string rationale, IReadOnlyList<Citation>? citations) =>
            {
                var citationList = citations ?? [];
                captured = new Suggestion
                {
                    Id           = 0,
                    ForDate      = snapshot.Today,
                    Action       = action,
                    QuantityHint = quantity_hint,
                    MaxPriceHint = max_price_hint,
                    Conviction   = conviction,
                    Rationale    = rationale,
                    CitationsJson = JsonSerializer.Serialize(citationList, JsonOpts.Strict),
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
            MaxOutputTokens = 1500,
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
}
