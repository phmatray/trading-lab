using TradingStrat.Domain.Common;
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
    /// <returns>Result containing list of recent activity events ordered by timestamp descending, or errors if retrieval fails.</returns>
    Task<Result<List<ActivityEvent>>> ExecuteAsync(int limit = 10);
}
