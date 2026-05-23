using TradyStrat.Application.Settings;
using TradyStrat.Domain.Settings.Anthropic;

namespace TradyStrat.TestKit.Settings;

/// <summary>
/// Test double for <see cref="IAnthropicSettingsRepository"/>. Pass a typed
/// AnthropicSettings record via the constructor; throws NotSupportedException
/// if no value was supplied (matches the existing FakeSettingsReader discipline).
/// </summary>
public sealed class FakeAnthropicSettingsRepository(AnthropicSettings? settings = null) : IAnthropicSettingsRepository
{
    private AnthropicSettings? _state = settings;

    public Task<AnthropicSettings> GetAsync(CancellationToken ct)
        => Task.FromResult(_state ?? throw new NotSupportedException(
            "AnthropicSettings not configured on FakeAnthropicSettingsRepository."));

    public Task SaveAsync(AnthropicSettings settings, CancellationToken ct)
    {
        _state = settings;
        return Task.CompletedTask;
    }
}
