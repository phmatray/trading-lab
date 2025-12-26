using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
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

    public async Task<List<ActivityEvent>> ExecuteAsync(int limit = 10)
    {
        return await _activityEventPort.GetRecentActivityAsync(limit);
    }
}
