using Microsoft.EntityFrameworkCore;
using System.Globalization;
using TradyStrat.Application.Settings;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Domain;
using TradyStrat.Domain.Settings.Anthropic;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Settings;

public sealed class EfAnthropicSettingsRepository(AppDbContext db, IClock clock) : IAnthropicSettingsRepository
{
    public async Task<AnthropicSettings> GetAsync(CancellationToken ct)
    {
        var rows = await db.Set<SettingEntry>()
            .Where(s => s.Key.StartsWith("anthropic."))
            .ToDictionaryAsync(s => s.Key, s => s.Value, ct);

        return new AnthropicSettings(
            AnthropicModel.Of(rows[SettingsKeys.AnthropicModel]),
            MaxTokens.Of(int.Parse(rows[SettingsKeys.AnthropicMaxTokens], CultureInfo.InvariantCulture)),
            ThinkingBudget.Of(int.Parse(rows[SettingsKeys.AnthropicThinkingBudget], CultureInfo.InvariantCulture)),
            MaxParallelSuggestions.Of(int.Parse(rows[SettingsKeys.AnthropicMaxParallelSuggestions], CultureInfo.InvariantCulture)));
    }

    public async Task<DateTime?> LastUpdatedAsync(CancellationToken ct)
    {
        return await db.Set<SettingEntry>()
            .Where(s => s.Key.StartsWith("anthropic."))
            .MaxAsync(s => (DateTime?)s.UpdatedAt, ct);
    }

    public async Task SaveAsync(AnthropicSettings settings, CancellationToken ct)
    {
        var now = clock.UtcNow();
        await UpsertAsync(SettingsKeys.AnthropicModel, settings.Model.Value, now, ct);
        await UpsertAsync(SettingsKeys.AnthropicMaxTokens, settings.MaxTokens.Value.ToString(CultureInfo.InvariantCulture), now, ct);
        await UpsertAsync(SettingsKeys.AnthropicThinkingBudget, settings.ThinkingBudget.Value.ToString(CultureInfo.InvariantCulture), now, ct);
        await UpsertAsync(SettingsKeys.AnthropicMaxParallelSuggestions, settings.MaxParallelSuggestions.Value.ToString(CultureInfo.InvariantCulture), now, ct);
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
            // instance and re-attach an updated copy via `with` — same pattern as UpdateGoalUseCase.
            db.Entry(existing).State = EntityState.Detached;
            db.Update(existing with { Value = value, UpdatedAt = now });
        }
    }
}
