using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Settings.Specifications;

namespace TradyStrat.Features.Settings.UseCases;

public sealed class ListInstrumentsUseCase(
    IReadRepositoryBase<Instrument> repo,
    ILogger<ListInstrumentsUseCase> log)
    : UseCaseBase<Unit, IReadOnlyList<Instrument>>(log)
{
    protected override async Task<IReadOnlyList<Instrument>> ExecuteCore(
        Unit input, CancellationToken ct)
        => await repo.ListAsync(new AllInstrumentsSpec(), ct);
}
