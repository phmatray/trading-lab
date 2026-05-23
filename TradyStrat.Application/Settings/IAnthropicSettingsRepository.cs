using TradyStrat.Domain.Settings.Anthropic;

namespace TradyStrat.Application.Settings;

public interface IAnthropicSettingsRepository
{
    Task<AnthropicSettings> GetAsync(CancellationToken ct);
    Task SaveAsync(AnthropicSettings settings, CancellationToken ct);
}
