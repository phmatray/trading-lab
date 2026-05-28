using TradingSignal.Core;
using TradingSignal.Llm.Abstractions;

namespace TradingSignal.Llm.Tests.Fakes;

internal sealed class FakeLlmCallStrategy : ILlmCallStrategy
{
    public string SystemPrompt { get; set; } = "fake-system-prompt";
    public Queue<LlmCallOutcome> Outcomes { get; } = new();
    public List<(string SystemPrompt, string UserMessage)> Calls { get; } = new();
    public int CallCount => Calls.Count;

    public Task<LlmCallOutcome> GenerateAsync(
        string systemPrompt, string userMessage, CancellationToken ct)
    {
        Calls.Add((systemPrompt, userMessage));
        if (Outcomes.Count == 0)
            throw new InvalidOperationException("FakeLlmCallStrategy: no outcomes queued");
        return Task.FromResult(Outcomes.Dequeue());
    }

    public void EnqueueSignal(TradeAction action, double confidence, string reason, string? reasoning = null)
        => Outcomes.Enqueue(new LlmCallOutcome(new RawSignal(action, confidence, reason, reasoning), reasoning));
}
