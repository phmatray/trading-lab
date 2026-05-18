using TradyStrat.Application.Settings.Config;
using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Settings.Config;

public sealed class SettingsService(
    IRepositoryBase<SettingEntry> repo,
    ISettingsRegistry registry,
    AppDbContext db,
    IClock clock) : ISettingsService
{
    public async Task<string> GetRawAsync(string key, CancellationToken ct)
    {
        var entry = await repo.GetByIdAsync(key, ct);
        return entry?.Value
            ?? throw new InvalidOperationException($"Setting '{key}' is missing from the Settings table.");
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken ct)
    {
        var raw = await GetRawAsync(key, ct);
        return (T)registry.Get(key).Parse(raw);
    }

    public async Task SetAsync(string key, string rawValue, CancellationToken ct)
    {
        var now = clock.UtcNow();
        var existing = await repo.GetByIdAsync(key, ct);
        if (existing is null)
        {
            await repo.AddAsync(new SettingEntry { Key = key, Value = rawValue, UpdatedAt = now }, ct);
            return;
        }
        // Detach the tracked instance so UpdateAsync can attach the updated copy without conflict
        // (same pattern as UpdateGoalUseCase).
        db.Entry(existing).State = EntityState.Detached;
        await repo.UpdateAsync(existing with { Value = rawValue, UpdatedAt = now }, ct);
    }

    public async Task<DateTime?> LastUpdatedAsync(IEnumerable<string> keys, CancellationToken ct)
    {
        var wanted = keys.ToHashSet();
        var all = await repo.ListAsync(ct);
        var stamps = all.Where(e => wanted.Contains(e.Key)).Select(e => e.UpdatedAt).ToList();
        return stamps.Count == 0 ? null : stamps.Max();
    }
}
