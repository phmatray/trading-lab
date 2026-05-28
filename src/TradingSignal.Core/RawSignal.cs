namespace TradingSignal.Core;

public sealed record RawSignal(
    TradeAction Action,
    double Confidence,
    string Reason,
    /// <summary>
    /// Chain-of-thought trace from a reasoning model (e.g. <c>reasoning_content</c>);
    /// null for instruct-model strategies.
    /// </summary>
    string? Reasoning = null);
