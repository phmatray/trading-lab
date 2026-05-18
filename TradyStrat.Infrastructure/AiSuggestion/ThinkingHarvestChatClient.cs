using Microsoft.Extensions.AI;

namespace TradyStrat.Infrastructure.AiSuggestion;

/// <summary>
/// Pulls "anthropic.thinking" text out of the model's response (top-level
/// or per-content) and mirrors it under <see cref="ThinkingTextKey"/> so
/// <c>SuggestionService</c> stays adapter-agnostic. Spec §6.2.
/// </summary>
internal sealed class ThinkingHarvestChatClient(IChatClient inner) : DelegatingChatClient(inner)
{
    /// <summary>Key under which the harvested thinking text appears on the response.</summary>
    public const string ThinkingTextKey = "trady.thinkingText";

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var response = await base.GetResponseAsync(messages, options, cancellationToken);
        var thinkingText = Extract(response);

        if (!string.IsNullOrEmpty(thinkingText))
        {
            response.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            response.AdditionalProperties[ThinkingTextKey] = thinkingText;
        }
        return response;
    }

    private static string? Extract(ChatResponse response)
    {
        if (response.AdditionalProperties is { } resp
            && resp.TryGetValue("anthropic.thinking", out var top)
            && top is string s1
            && !string.IsNullOrEmpty(s1)) return s1;

        var sb = new System.Text.StringBuilder();
        foreach (var msg in response.Messages)
        foreach (var content in msg.Contents.OfType<TextContent>())
        {
            if (content.AdditionalProperties?.TryGetValue("anthropic.thinking", out var v) == true
                && v is string s2)
                sb.AppendLine(s2);
        }
        return sb.Length > 0 ? sb.ToString().TrimEnd() : null;
    }
}
