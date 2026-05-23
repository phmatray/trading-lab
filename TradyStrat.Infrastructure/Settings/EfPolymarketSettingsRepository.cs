using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using TradyStrat.Application.Settings;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;
using TradyStrat.Domain.Settings.Polymarket;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Settings;

public sealed class EfPolymarketSettingsRepository(AppDbContext db, IClock clock) : IPolymarketSettingsRepository
{
    public async Task<PolymarketSettings> GetAsync(CancellationToken ct)
    {
        var rows = await db.Set<SettingEntry>()
            .Where(s => s.Key.StartsWith("polymarket."))
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        return new PolymarketSettings(
            SearchQueries.Of(JsonSerializer.Deserialize<string[]>(rows[SettingsKeys.PolymarketSearchQueries]) ?? Array.Empty<string>()),
            MaxMarkets.Of(int.Parse(rows[SettingsKeys.PolymarketMaxMarkets], CultureInfo.InvariantCulture)),
            MinVolumeUsd.Of(decimal.Parse(rows[SettingsKeys.PolymarketMinVolumeUsd], CultureInfo.InvariantCulture)),
            MaxHorizonDays.Of(int.Parse(rows[SettingsKeys.PolymarketMaxHorizonDays], CultureInfo.InvariantCulture)));
    }

    public async Task SaveAsync(PolymarketSettings settings, CancellationToken ct)
    {
        var now = clock.UtcNow();
        await UpsertAsync(SettingsKeys.PolymarketSearchQueries, JsonSerializer.Serialize(settings.SearchQueries.Values), now, ct);
        await UpsertAsync(SettingsKeys.PolymarketMaxMarkets, settings.MaxMarkets.Value.ToString(CultureInfo.InvariantCulture), now, ct);
        await UpsertAsync(SettingsKeys.PolymarketMinVolumeUsd, settings.MinVolumeUsd.Value.ToString(CultureInfo.InvariantCulture), now, ct);
        await UpsertAsync(SettingsKeys.PolymarketMaxHorizonDays, settings.MaxHorizonDays.Value.ToString(CultureInfo.InvariantCulture), now, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task UpsertAsync(string key, string value, DateTime now, CancellationToken ct)
    {
        var existing = await db.Set<SettingEntry>().FindAsync([key], ct);
        if (existing is null)
        {
            db.Add(new SettingEntry { Key = key, Value = value, UpdatedAt = now });
        }
        else
        {
            // SettingEntry is an immutable record (init-only props); detach the tracked
            // instance and re-attach an updated copy via `with` — same pattern as
            // SettingsService.SetAsync / UpdateGoalUseCase.
            db.Entry(existing).State = EntityState.Detached;
            db.Update(existing with { Value = value, UpdatedAt = now });
        }
    }
}
