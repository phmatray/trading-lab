using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Settings;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Settings.Tickers;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Settings;

public sealed class EfFocusTickerRepository(
    AppDbContext db,
    IInstrumentRepository instruments,
    IClock clock) : IFocusTickerRepository
{
    public async Task<FocusTicker> GetAsync(CancellationToken ct)
    {
        var entry = await db.Set<SettingEntry>().FindAsync([SettingsKeys.TickersFocus], ct)
            ?? throw new InvalidOperationException("Focus ticker missing from Settings.");
        return FocusTicker.Of(entry.Value);
    }

    public async Task<DateTime?> LastUpdatedAsync(CancellationToken ct)
    {
        var entry = await db.Set<SettingEntry>().FindAsync([SettingsKeys.TickersFocus], ct);
        return entry?.UpdatedAt;
    }

    public async Task SaveAsync(FocusTicker ticker, CancellationToken ct)
    {
        // Cross-aggregate invariant: focus ticker must reference a known Instrument.
        var match = await instruments.FindByTickerAsync(ticker.Value, ct);
        if (match is null)
            throw new SettingValidationException(
                $"Focus ticker '{ticker.Value}' does not match any registered instrument.");

        var now = clock.UtcNow();
        var existing = await db.Set<SettingEntry>().FindAsync([SettingsKeys.TickersFocus], ct);
        if (existing is null)
        {
            db.Add(new SettingEntry { Key = SettingsKeys.TickersFocus, Value = ticker.Value, UpdatedAt = now });
        }
        else
        {
            // SettingEntry is an immutable record (init-only props); detach the tracked
            // instance and re-attach an updated copy via `with` — same pattern as Tasks 5/6.
            db.Entry(existing).State = EntityState.Detached;
            db.Update(existing with { Value = ticker.Value, UpdatedAt = now });
        }
        await db.SaveChangesAsync(ct);
    }
}
