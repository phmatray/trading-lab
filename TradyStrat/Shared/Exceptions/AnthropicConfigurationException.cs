namespace TradyStrat.Shared.Exceptions;

public sealed class AnthropicConfigurationException(string message)
    : TradyStratException(message);
