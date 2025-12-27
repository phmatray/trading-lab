using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for retrieving recent activity events for the dashboard.
/// </summary>
public class GetRecentActivityUseCase : IGetRecentActivityUseCase
{
    private readonly IActivityEventPort _activityEventPort;

    public GetRecentActivityUseCase(IActivityEventPort activityEventPort)
    {
        _activityEventPort = activityEventPort;
    }

    public async Task<Result<List<ActivityEvent>>> ExecuteAsync(int limit = 10)
    {
        try
        {
            List<ActivityEvent> events = await _activityEventPort.GetRecentActivityAsync(limit);
            return Result<List<ActivityEvent>>.Success(events);
        }
        catch (Exception ex)
        {
            return Result<List<ActivityEvent>>.Failure(
                Error.BusinessRule($"Failed to retrieve recent activity: {ex.Message}", "RECENT_ACTIVITY_FAILED"));
        }
    }
}
