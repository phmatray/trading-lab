using TradingSignal.Core;

namespace TradingSignal.Llm.Abstractions;

public interface ILlmCallStrategy
{
    /// <summary>
    /// The system prompt this strategy uses. Exposed so the orchestrator
    /// (LlmSignalGenerator) can include it in the cache key.
    /// </summary>
    string SystemPrompt { get; }

    Task<LlmCallOutcome> GenerateAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken ct);
}

public sealed record LlmCallOutcome(RawSignal Signal, string? ReasoningContent);
