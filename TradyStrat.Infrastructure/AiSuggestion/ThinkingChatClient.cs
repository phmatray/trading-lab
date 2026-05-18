using Anthropic.SDK.Extensions;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.AI;
using TradyStrat.Application.Settings.Config;

namespace TradyStrat.Infrastructure.AiSuggestion;

/// <summary>
/// Reads <c>anthropic.thinkingBudget</c> and applies adaptive thinking
/// (the API supported by Opus 4.6+ and Sonnet 4.6+; the legacy fixed-budget
/// <c>WithThinking(int)</c> only works on Claude 3.7 Sonnet).
///
/// The existing <c>ThinkingBudget</c> int setting now maps to a
/// <see cref="ThinkingEffort"/> tier:
///   &lt;= 2048  → Low
///   &lt;= 6144  → Medium
///   &lt;= 10240 → High
///   else     → Max
/// </summary>
internal sealed class ThinkingChatClient(IChatClient inner, ISettingsReader settings)
    : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var ai = await settings.AnthropicAsync(cancellationToken);
        var effort = MapEffort(ai.ThinkingBudget);
        var withThinking = (options ?? new ChatOptions()).WithAdaptiveThinking(effort);
        return await base.GetResponseAsync(messages, withThinking, cancellationToken);
    }

    private static ThinkingEffort MapEffort(int budget) => budget switch
    {
        <=  2048 => ThinkingEffort.low,
        <=  6144 => ThinkingEffort.medium,
        <= 10240 => ThinkingEffort.high,
        _        => ThinkingEffort.max,
    };
}
