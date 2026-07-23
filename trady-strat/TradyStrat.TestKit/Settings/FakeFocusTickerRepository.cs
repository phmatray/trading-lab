using TradyStrat.Application.Settings;
using TradyStrat.Domain.Settings.Tickers;

namespace TradyStrat.TestKit.Settings;

/// <summary>
/// Test double for <see cref="IFocusTickerRepository"/>. Defaults to "CON3.L".
/// SaveAsync stores in-memory; no Instrument-existence check.
/// </summary>
public sealed class FakeFocusTickerRepository(string ticker = "CON3.L") : IFocusTickerRepository
{
    private FocusTicker _state = FocusTicker.Of(ticker);

    public Task<FocusTicker> GetAsync(CancellationToken ct) => Task.FromResult(_state);

    public Task SaveAsync(FocusTicker ticker, CancellationToken ct)
    {
        _state = ticker;
        _lastUpdated = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    private DateTime? _lastUpdated;

    public Task<DateTime?> LastUpdatedAsync(CancellationToken ct) => Task.FromResult(_lastUpdated);
}
