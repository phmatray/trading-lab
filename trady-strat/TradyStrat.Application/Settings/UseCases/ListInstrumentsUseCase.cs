using TradyStrat.Application.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Application.Settings.UseCases;

public sealed class ListInstrumentsUseCase(
    IInstrumentRepository repo,
    ILogger<ListInstrumentsUseCase> log)
    : UseCaseBase<Unit, IReadOnlyList<Instrument>>(log)
{
    protected override Task<IReadOnlyList<Instrument>> ExecuteCore(
        Unit input, CancellationToken ct)
        => repo.ListAsync(ct);
}
