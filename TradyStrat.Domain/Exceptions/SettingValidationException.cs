namespace TradyStrat.Domain.Exceptions;

public sealed class SettingValidationException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
