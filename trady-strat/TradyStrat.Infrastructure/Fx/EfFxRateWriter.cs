using TradyStrat.Application.Fx;
using TradyStrat.Domain;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Fx;

internal sealed class EfFxRateWriter(AppDbContext db) : IFxRateWriter
{
    public async Task AppendAsync(IReadOnlyList<FxRate> rates, CancellationToken ct)
    {
        db.FxRates.AddRange(rates);
        await db.SaveChangesAsync(ct);
    }
}
