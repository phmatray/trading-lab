using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for retrieving recent activity events for the dashboard.
/// Uses BaseUseCase to eliminate try-catch boilerplate.
/// </summary>
public class GetRecentActivityUseCase : BaseUseCase<int, List<ActivityEvent>>, IGetRecentActivityUseCase
{
    private readonly IActivityEventPort _activityEventPort;

    public GetRecentActivityUseCase(IActivityEventPort activityEventPort)
    {
        _activityEventPort = activityEventPort;
    }

    public Task<Result<List<ActivityEvent>>> ExecuteAsync(int limit = 10)
        => base.ExecuteAsync(limit, ExecuteCoreAsync, ErrorCodes.Dashboard.StatsFailed);

    private async Task<List<ActivityEvent>> ExecuteCoreAsync(int limit)
    {
        return await _activityEventPort.GetRecentActivityAsync(limit);
    }
}
