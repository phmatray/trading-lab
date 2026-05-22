using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.PriceFeed;

namespace TradyStrat.Application.Dashboard.Navigation;

public sealed class EntryNavigationService(
    IPriceBarReadRepository bars,
    ISettingsReader settings) : IEntryNavigationService
{
    public async Task<DateOnly> EarliestAsync(CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var bar = await bars.EarliestAsync(focusTicker, ct);
        return bar?.Date ?? throw new NoTradingDaysException();
    }

    public async Task<DateOnly> LatestAsync(CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var bar = await bars.LatestAsync(focusTicker, ct);
        return bar?.Date ?? throw new NoTradingDaysException();
    }

    public async Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var bar = await bars.LatestBeforeAsync(focusTicker, current, ct);
        return bar?.Date;
    }

    public async Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var bar = await bars.EarliestAfterAsync(focusTicker, current, ct);
        return bar?.Date;
    }

    public async Task<DateOnly> ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var onOrBefore = await bars.ListAsOfAsync(focusTicker, requested, ct);
        if (onOrBefore.Count == 0) throw new NoTradingDaysException();
        return onOrBefore[^1].Date;
    }
}
