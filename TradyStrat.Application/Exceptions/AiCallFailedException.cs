using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Application.Exceptions;

/// <summary>
/// Abstract failure mode at the IAiClient port boundary. Vendor-specific
/// adapters (Anthropic, future Gemini, …) inherit from this so Application
/// catch sites can use one abstract type without importing vendor packages.
/// </summary>
public class AiCallFailedException : TradyStratException
{
    public AiCallFailedException(string message) : base(message) { }
    public AiCallFailedException(string message, Exception? inner = null) : base(message, inner) { }
}
