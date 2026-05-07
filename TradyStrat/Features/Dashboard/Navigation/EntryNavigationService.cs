using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.PriceFeed.Specifications;

namespace TradyStrat.Features.Dashboard.Navigation;

public sealed class EntryNavigationService(IReadRepositoryBase<PriceBar> bars)
    : IEntryNavigationService
{
    private const string FocusTicker = "CON3.L";

    public async Task<DateOnly> EarliestAsync(CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new EarliestPriceBarSpec(FocusTicker), ct);
        return bar?.Date ?? throw new NoTradingDaysException();
    }

    public async Task<DateOnly> LatestAsync(CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new LatestPriceBarSpec(FocusTicker), ct);
        return bar?.Date ?? throw new NoTradingDaysException();
    }

    public async Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new PriceBarBeforeSpec(FocusTicker, current), ct);
        return bar?.Date;
    }

    public async Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new PriceBarAfterSpec(FocusTicker, current), ct);
        return bar?.Date;
    }

    public async Task<DateOnly> ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct)
    {
        var onOrBefore = await bars.ListAsync(
            new PriceBarsAsOfSpec(FocusTicker, requested), ct);
        if (onOrBefore.Count == 0) throw new NoTradingDaysException();
        return onOrBefore[^1].Date;
    }
}
