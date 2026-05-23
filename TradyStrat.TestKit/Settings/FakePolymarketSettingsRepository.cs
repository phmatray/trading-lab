using TradyStrat.Application.Settings;
using TradyStrat.Domain.Settings.Polymarket;

namespace TradyStrat.TestKit.Settings;

/// <summary>
/// Test double for <see cref="IPolymarketSettingsRepository"/>. Pass a typed
/// PolymarketSettings record via the constructor; throws NotSupportedException
/// if no value was supplied (matches the existing FakeSettingsReader discipline).
/// </summary>
public sealed class FakePolymarketSettingsRepository(PolymarketSettings? settings = null) : IPolymarketSettingsRepository
{
    private PolymarketSettings? _state = settings;

    public Task<PolymarketSettings> GetAsync(CancellationToken ct)
        => Task.FromResult(_state ?? throw new NotSupportedException(
            "PolymarketSettings not configured on FakePolymarketSettingsRepository."));

    public Task SaveAsync(PolymarketSettings settings, CancellationToken ct)
    {
        _state = settings;
        return Task.CompletedTask;
    }
}
