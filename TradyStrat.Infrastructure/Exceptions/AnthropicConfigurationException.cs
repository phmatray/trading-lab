using TradyStrat.Domain.Exceptions;
namespace TradyStrat.Infrastructure.Exceptions;

public sealed class AnthropicConfigurationException(string message)
    : TradyStratException(message);
