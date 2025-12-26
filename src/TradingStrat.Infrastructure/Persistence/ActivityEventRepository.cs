using Microsoft.EntityFrameworkCore;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Infrastructure.Persistence.EfCore;

namespace TradingStrat.Infrastructure.Persistence;

/// <summary>
/// Repository for persisting and retrieving activity events.
/// Implements IActivityEventPort using Entity Framework Core.
/// </summary>
public class ActivityEventRepository : IActivityEventPort
{
    private readonly TradingContext _context;

    public ActivityEventRepository(TradingContext context)
    {
        _context = context;
    }

    public async Task<ActivityEvent> RecordActivityAsync(ActivityEvent activityEvent)
    {
        _context.ActivityEvents.Add(activityEvent);
        await _context.SaveChangesAsync();
        return activityEvent;
    }

    public async Task<List<ActivityEvent>> GetRecentActivityAsync(int limit = 10, string? eventType = null)
    {
        var query = _context.ActivityEvents.AsQueryable();

        if (!string.IsNullOrEmpty(eventType))
        {
            query = query.Where(e => e.EventType == eventType);
        }

        return await query
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetActivityCountAsync()
    {
        return await _context.ActivityEvents.CountAsync();
    }
}
