using TradyStrat.Domain.Settings.Anthropic;

namespace TradyStrat.Application.Settings;

public interface IAnthropicSettingsRepository
{
    Task<AnthropicSettings> GetAsync(CancellationToken ct);
    Task SaveAsync(AnthropicSettings settings, CancellationToken ct);

    /// <summary>MAX(UpdatedAt) over the Anthropic setting rows, or null if none exist. Used by the Settings form to show "last saved at".</summary>
    Task<DateTime?> LastUpdatedAsync(CancellationToken ct);
}
