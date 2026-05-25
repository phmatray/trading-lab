using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.Settings;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;
using TradyStrat.Infrastructure.Data;

namespace TradyStrat.Infrastructure.Settings;

public sealed class EfInstrumentRepository(AppDbContext db) : IInstrumentRepository
{
    public Task<Instrument?> GetAsync(InstrumentId id, CancellationToken ct)
        => db.Instruments.SingleOrDefaultAsync(i => i.Id == id, ct);

    public Task<Instrument?> FindByTickerAsync(string ticker, CancellationToken ct)
    {
        var normalized = (ticker ?? "").Trim().ToUpperInvariant();
        return db.Instruments.SingleOrDefaultAsync(i => i.Ticker == normalized, ct);
    }

    public async Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken ct)
        => await db.Instruments.OrderBy(i => i.Ticker).ToListAsync(ct);

    public async Task<IReadOnlyList<IDomainEvent>> AddAsync(Instrument instrument, CancellationToken ct)
    {
        var dup = await FindByTickerAsync(instrument.Ticker, ct);
        if (dup is not null)
            throw new DuplicateInstrumentException(
                $"Instrument '{instrument.Ticker}' is already tracked.");

        db.Instruments.Add(instrument);
        await db.SaveChangesAsync(ct);
        return instrument.DequeueDomainEvents();
    }
}
