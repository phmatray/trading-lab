using TradyStrat.Application.Exceptions;

namespace TradyStrat.Infrastructure.Exceptions;

public sealed class AnthropicCallFailedException(string message, Exception? inner = null)
    : AiCallFailedException(message, inner);
