using TradyStrat.Features.Settings.Config;

namespace TradyStrat.Tests.Settings;

/// <summary>
/// Test double for <see cref="ISettingsReader"/>. The focus ticker defaults to "CON3.L";
/// the Anthropic / Polymarket members throw unless explicitly supplied. <see cref="LastUpdatedAsync"/>
/// returns null. Pass a different ticker / typed records via the constructor when a test needs them.
/// </summary>
public sealed class FakeSettingsReader(
    string focusTicker = "CON3.L",
    AnthropicSettings? anthropic = null,
    PolymarketSettings? polymarket = null) : ISettingsReader
{
    public Task<AnthropicSettings> AnthropicAsync(CancellationToken ct)
        => Task.FromResult(anthropic ?? throw new NotSupportedException("AnthropicSettings not configured on FakeSettingsReader."));

    public Task<PolymarketSettings> PolymarketAsync(CancellationToken ct)
        => Task.FromResult(polymarket ?? throw new NotSupportedException("PolymarketSettings not configured on FakeSettingsReader."));

    public Task<string> FocusTickerAsync(CancellationToken ct) => Task.FromResult(focusTicker);

    public Task<DateTime?> LastUpdatedAsync(IEnumerable<string> keys, CancellationToken ct) => Task.FromResult<DateTime?>(null);
}
