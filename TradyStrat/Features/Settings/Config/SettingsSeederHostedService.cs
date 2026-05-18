using TradyStrat.Application.Settings.Config;
using Microsoft.EntityFrameworkCore;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Features.Settings.Config;

/// <summary>
/// On host start (i.e. after DatabaseModule has applied migrations), back-fills any
/// registry key that has no row in the Settings table with its default value.
/// Existing rows are never touched, so it's safe to run repeatedly and never clobbers
/// a user's customisation.
/// </summary>
public sealed class SettingsSeederHostedService(
    IServiceScopeFactory scopes,
    ISettingsRegistry registry) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();

        var present = await db.Settings.Select(e => e.Key).ToHashSetAsync(cancellationToken);
        var now = clock.UtcNow();
        var missing = registry.All.Values
            .Where(d => !present.Contains(d.Key))
            .Select(d => new SettingEntry { Key = d.Key, Value = d.DefaultRaw, UpdatedAt = now })
            .ToList();

        if (missing.Count == 0) return;
        db.Settings.AddRange(missing);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
