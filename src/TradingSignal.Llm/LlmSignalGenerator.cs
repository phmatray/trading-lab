using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TradingSignal.Core;
using TradingSignal.Core.Abstractions;
using TradingSignal.Llm.Abstractions;
using TradingSignal.Llm.Caching;
using TradingSignal.Llm.Prompts;

namespace TradingSignal.Llm;

public sealed partial class LlmSignalGenerator(
    ILlmCallStrategy strategy,
    LmStudioOptions options,
    ILlmResponseCache cache,
    ILogger<LlmSignalGenerator>? logger = null)
    : ISignalGenerator
{
    private readonly ILogger<LlmSignalGenerator> _logger = logger ?? NullLogger<LlmSignalGenerator>.Instance;

    public async Task<RawSignal> GenerateAsync(
        FeatureSet features, IReadOnlyList<FewShotCase> memory, CancellationToken ct)
    {
        string systemPrompt = strategy.SystemPrompt;
        string userMessage = PromptBuilder.BuildUserMessage(features, memory, options.MaxFewShot);
        string cacheKey = ComputeCacheKey(options.ModelId, options.ReasoningEffort, systemPrompt, userMessage);

        RawSignal? cached = await cache.TryGetAsync(cacheKey, ct).ConfigureAwait(false);
        if (cached is not null)
        {
            LogCacheHit(_logger, features.AsOfUtc, features.Symbol);
            return cached;
        }

        LlmCallOutcome outcome = await strategy.GenerateAsync(systemPrompt, userMessage, ct).ConfigureAwait(false);
        RawSignal final = outcome.Signal with { Reasoning = outcome.ReasoningContent };
        await cache.SetAsync(cacheKey, final, ct).ConfigureAwait(false);
        return final;
    }

    private static string ComputeCacheKey(
        string modelId, string reasoningEffort, string systemPrompt, string userMessage)
    {
        byte[] payload = Encoding.UTF8.GetBytes($"{modelId} {reasoningEffort} {systemPrompt} {userMessage}");
        byte[] hash = SHA256.HashData(payload);
        return Convert.ToHexStringLower(hash);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "LLM cache hit for {AsOf} {Symbol}")]
    private static partial void LogCacheHit(ILogger logger, DateTime asOf, string symbol);
}
