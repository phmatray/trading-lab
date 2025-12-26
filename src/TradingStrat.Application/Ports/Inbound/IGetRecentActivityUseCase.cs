using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Inbound;

/// <summary>
/// Use case for retrieving recent activity events.
/// </summary>
public interface IGetRecentActivityUseCase
{
    /// <summary>
    /// Executes the use case to retrieve recent activity events.
    /// </summary>
    /// <param name="limit">Maximum number of events to return (default 10).</param>
    /// <returns>List of recent activity events ordered by timestamp descending.</returns>
    Task<List<ActivityEvent>> ExecuteAsync(int limit = 10);
}
