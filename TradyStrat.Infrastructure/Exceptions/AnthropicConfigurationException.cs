using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Infrastructure.Exceptions;

public sealed class AnthropicConfigurationException(string message)
    : TradyStratException(message);
