using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.PriceFeed.Specifications;
using TradyStrat.Features.Settings.Config;

namespace TradyStrat.Features.Dashboard.Navigation;

public sealed class EntryNavigationService(
    IReadRepositoryBase<PriceBar> bars,
    ISettingsReader settings) : IEntryNavigationService
{
    public async Task<DateOnly> EarliestAsync(CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var bar = await bars.FirstOrDefaultAsync(new EarliestPriceBarSpec(focusTicker), ct);
        return bar?.Date ?? throw new NoTradingDaysException();
    }

    public async Task<DateOnly> LatestAsync(CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var bar = await bars.FirstOrDefaultAsync(new LatestPriceBarSpec(focusTicker), ct);
        return bar?.Date ?? throw new NoTradingDaysException();
    }

    public async Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var bar = await bars.FirstOrDefaultAsync(new PriceBarBeforeSpec(focusTicker, current), ct);
        return bar?.Date;
    }

    public async Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var bar = await bars.FirstOrDefaultAsync(new PriceBarAfterSpec(focusTicker, current), ct);
        return bar?.Date;
    }

    public async Task<DateOnly> ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct)
    {
        var focusTicker = await settings.FocusTickerAsync(ct);
        var onOrBefore = await bars.ListAsync(
            new PriceBarsAsOfSpec(focusTicker, requested), ct);
        if (onOrBefore.Count == 0) throw new NoTradingDaysException();
        return onOrBefore[^1].Date;
    }
}
