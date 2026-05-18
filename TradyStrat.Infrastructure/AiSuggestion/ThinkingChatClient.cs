using Anthropic.SDK.Extensions;
using Microsoft.Extensions.AI;
using TradyStrat.Application.Settings.Config;

namespace TradyStrat.Infrastructure.AiSuggestion;

/// <summary>
/// Reads the configured <c>anthropic.thinkingBudget</c> setting and applies
/// <see cref="ChatOptionsExtensions.WithThinking(ChatOptions, int)"/> on every call.
/// Spec §6.1.
/// </summary>
internal sealed class ThinkingChatClient(IChatClient inner, ISettingsReader settings)
    : DelegatingChatClient(inner)
{
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var ai = await settings.AnthropicAsync(cancellationToken);
        var withThinking = (options ?? new ChatOptions()).WithThinking(ai.ThinkingBudget);
        return await base.GetResponseAsync(messages, withThinking, cancellationToken);
    }
}
