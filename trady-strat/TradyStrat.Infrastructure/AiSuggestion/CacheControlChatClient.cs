using Microsoft.Extensions.AI;
using AnthropicMessaging = Anthropic.SDK.Messaging;

namespace TradyStrat.Infrastructure.AiSuggestion;

/// <summary>
/// Decorator that translates a <see cref="CacheBreakpointKey"/> flag on
/// <see cref="TextContent.AdditionalProperties"/> into the SDK-native
/// <see cref="CacheControl"/> marker so <c>Anthropic.SDK 5.10</c>'s
/// Microsoft.Extensions.AI bridge emits <c>cache_control: { type: ephemeral }</c>
/// on the corresponding content block.
///
/// Implementation path: see docs/superpowers/notes/2026-05-18-cache-control-spike.md.
/// Phase 0 spike was deferred — Path B chosen (typed CacheControl in
/// AdditionalProperties under the SDK's documented "anthropic.cache_control" key).
/// If runtime verification later shows Path B doesn't round-trip, swap the body
/// per the note's fallback options.
/// </summary>
internal sealed class CacheControlChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    /// <summary>
    /// SuggestionService attaches this key (value <c>true</c>) on the TextContent
    /// whose tokens should anchor the cacheable prefix.
    /// </summary>
    public const string CacheBreakpointKey = "trady.cacheBreakpoint";

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        foreach (var msg in messages)
        foreach (var content in msg.Contents.OfType<TextContent>())
        {
            if (content.AdditionalProperties is { } props
                && props.TryGetValue(CacheBreakpointKey, out var v)
                && v is true)
            {
                props["anthropic.cache_control"] = new AnthropicMessaging.CacheControl
                {
                    Type = AnthropicMessaging.CacheControlType.ephemeral,
                };
            }
        }
        return base.GetResponseAsync(messages, options, cancellationToken);
    }
}
