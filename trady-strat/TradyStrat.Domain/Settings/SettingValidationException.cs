using TradyStrat.Domain.SeedWork;

namespace TradyStrat.Domain.Settings;

public sealed class SettingValidationException(string message, Exception? inner = null)
    : TradyStratException(message, inner);
