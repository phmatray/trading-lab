using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.PriceFeed.Specifications;

namespace TradyStrat.Features.Dashboard.Navigation;

public sealed class EntryNavigationService(
    IReadRepositoryBase<PriceBar> bars,
    IConfiguration config) : IEntryNavigationService
{
    private readonly string _focusTicker = config["Tickers:Focus"]
        ?? throw new InvalidOperationException("Tickers:Focus is not configured.");

    public async Task<DateOnly> EarliestAsync(CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new EarliestPriceBarSpec(_focusTicker), ct);
        return bar?.Date ?? throw new NoTradingDaysException();
    }

    public async Task<DateOnly> LatestAsync(CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new LatestPriceBarSpec(_focusTicker), ct);
        return bar?.Date ?? throw new NoTradingDaysException();
    }

    public async Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new PriceBarBeforeSpec(_focusTicker, current), ct);
        return bar?.Date;
    }

    public async Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct)
    {
        var bar = await bars.FirstOrDefaultAsync(new PriceBarAfterSpec(_focusTicker, current), ct);
        return bar?.Date;
    }

    public async Task<DateOnly> ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct)
    {
        var onOrBefore = await bars.ListAsync(
            new PriceBarsAsOfSpec(_focusTicker, requested), ct);
        if (onOrBefore.Count == 0) throw new NoTradingDaysException();
        return onOrBefore[^1].Date;
    }
}
