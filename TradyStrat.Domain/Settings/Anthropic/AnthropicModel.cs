using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Domain.Settings.Anthropic;

public readonly record struct AnthropicModel(string Value)
{
    public static AnthropicModel Of(string raw)
    {
        var trimmed = (raw ?? "").Trim();
        if (trimmed.Length == 0)
            throw new SettingValidationException("Anthropic model cannot be empty.");
        return new AnthropicModel(trimmed);
    }
    public override string ToString() => Value;
}
