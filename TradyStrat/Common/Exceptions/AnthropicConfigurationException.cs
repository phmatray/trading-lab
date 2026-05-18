using TradyStrat.Domain.Exceptions;
namespace TradyStrat.Common.Exceptions;

public sealed class AnthropicConfigurationException(string message)
    : TradyStratException(message);
