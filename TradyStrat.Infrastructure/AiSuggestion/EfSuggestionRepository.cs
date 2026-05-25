using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.AiSuggestion;

public sealed class EfSuggestionRepository(AppDbContext db) : ISuggestionRepository
{
    private IQueryable<Suggestion> WithCitations()
        => db.Suggestions.Include(s => s.Citations);

    public async Task<Suggestion?> GetForAsync(InstrumentId instrumentId, DateOnly date, CancellationToken ct)
        => await WithCitations()
            .SingleOrDefaultAsync(s => s.InstrumentId == instrumentId && s.ForDate == date, ct);

    public async Task<IReadOnlyList<Suggestion>> ListForAsync(
        InstrumentId instrumentId, DateRange range, CancellationToken ct)
        => await WithCitations()
            .Where(s => s.InstrumentId == instrumentId
                       && s.ForDate >= range.From && s.ForDate <= range.To)
            .OrderBy(s => s.ForDate)
            .ToListAsync(ct);

    public async Task<Suggestion?> LatestForAsync(InstrumentId instrumentId, CancellationToken ct)
        => await WithCitations()
            .Where(s => s.InstrumentId == instrumentId)
            .OrderByDescending(s => s.ForDate)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Suggestion>> QueryAsync(
        InstrumentId? instrumentId,
        DateRange? range,
        SuggestionAction? action,
        int take,
        CancellationToken ct)
    {
        var q = WithCitations();
        if (instrumentId is { } iid) q = q.Where(s => s.InstrumentId == iid);
        if (range is { } r) q = q.Where(s => s.ForDate >= r.From && s.ForDate <= r.To);
        if (action is { } a) q = q.Where(s => s.Action == a);
        return await q.OrderByDescending(s => s.ForDate).Take(take).ToListAsync(ct);
    }

    public async Task<Suggestion?> PriorToAsync(
        InstrumentId instrumentId, DateOnly before, CancellationToken ct)
        => await WithCitations()
            .Where(s => s.InstrumentId == instrumentId && s.ForDate < before)
            .OrderByDescending(s => s.ForDate)
            .ThenByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Suggestion>> RecentForAsync(
        InstrumentId instrumentId, DateOnly asOf, int count, CancellationToken ct)
        => await WithCitations()
            .Where(s => s.InstrumentId == instrumentId && s.ForDate <= asOf)
            .OrderByDescending(s => s.ForDate)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<IDomainEvent>> AddAsync(Suggestion suggestion, CancellationToken ct)
    {
        db.Suggestions.Add(suggestion);
        await db.SaveChangesAsync(ct);
        return suggestion.DequeueDomainEvents();
    }

    public async Task RemoveAsync(Suggestion suggestion, CancellationToken ct)
    {
        db.Suggestions.Remove(suggestion);
        await db.SaveChangesAsync(ct);
    }
}
