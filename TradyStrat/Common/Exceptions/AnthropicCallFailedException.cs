using TradyStrat.Domain.Exceptions;
namespace TradyStrat.Common.Exceptions;

public sealed class AnthropicCallFailedException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
