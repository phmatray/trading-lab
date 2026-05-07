using Ardalis.Specification;
using TradyStrat.Common.UseCases;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;

namespace TradyStrat.Features.Trades.UseCases;

public sealed record DeleteTradeInput(int Id);

public sealed class DeleteTradeUseCase(
    IRepositoryBase<Trade> repo, ILogger<DeleteTradeUseCase> log)
    : UseCaseBase<DeleteTradeInput, Unit>(log)
{
    protected override async Task<Unit> ExecuteCore(DeleteTradeInput input, CancellationToken ct)
    {
        // GetByIdAsync tracks the entity; DeleteAsync can work with the tracked instance directly.
        var existing = await repo.GetByIdAsync(input.Id, ct)
            ?? throw new TradeValidationException($"Trade {input.Id} not found.");

        await repo.DeleteAsync(existing, ct);
        return Unit.Value;
    }
}
