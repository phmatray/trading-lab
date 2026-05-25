using TradyStrat.Domain;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Settings;

public interface IInstrumentRepository
{
    Task<Instrument?> GetAsync(InstrumentId id, CancellationToken ct);
    Task<Instrument?> FindByTickerAsync(string ticker, CancellationToken ct);
    Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken ct);

    /// <summary>Throws DuplicateInstrumentException when Ticker already exists. Returns drained domain events.</summary>
    Task<IReadOnlyList<IDomainEvent>> AddAsync(Instrument instrument, CancellationToken ct);
}
