using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Infrastructure.AiSuggestion;

internal sealed class CitationsJsonDualWriteInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        StampCitationsJson(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        StampCitationsJson(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static void StampCitationsJson(DbContext? ctx)
    {
        if (ctx is null) return;

        foreach (var entry in ctx.ChangeTracker.Entries<Suggestion>())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            var json = JsonSerializer.Serialize(entry.Entity.Citations, Json);
            entry.Property("CitationsJson").CurrentValue = json;
        }
    }
}
