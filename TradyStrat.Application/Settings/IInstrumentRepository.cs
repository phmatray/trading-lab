using TradyStrat.Domain;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Settings;

/// <summary>
/// Per-aggregate repository for Instrument. AddAsync enforces system-wide
/// ticker uniqueness at the write path (the aggregate itself can't check
/// uniqueness — that spans aggregates).
/// </summary>
public interface IInstrumentRepository
{
    Task<Instrument?> GetAsync(InstrumentId id, CancellationToken ct);

    /// <summary>Returns null when no instrument matches the given ticker.</summary>
    Task<Instrument?> FindByTickerAsync(string ticker, CancellationToken ct);

    Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken ct);

    /// <summary>Throws DuplicateInstrumentException when Ticker already exists.</summary>
    Task AddAsync(Instrument instrument, CancellationToken ct);
}
