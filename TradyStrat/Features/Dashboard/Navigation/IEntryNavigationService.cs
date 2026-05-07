namespace TradyStrat.Features.Dashboard.Navigation;

public interface IEntryNavigationService
{
    Task<DateOnly>  EarliestAsync(CancellationToken ct);
    Task<DateOnly>  LatestAsync(CancellationToken ct);
    Task<DateOnly?> PreviousAsync(DateOnly current, CancellationToken ct);
    Task<DateOnly?> NextAsync(DateOnly current, CancellationToken ct);
    Task<DateOnly>  ResolveOrFallbackAsync(DateOnly requested, CancellationToken ct);
}
