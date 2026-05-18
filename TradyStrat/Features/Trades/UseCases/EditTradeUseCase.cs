using Ardalis.Specification;
using Microsoft.EntityFrameworkCore;
using TradyStrat.Application.UseCases;
using TradyStrat.Data;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;

namespace TradyStrat.Features.Trades.UseCases;

public sealed record EditTradeInput(
    int Id, int InstrumentId, DateOnly ExecutedOn, TradeSide Side,
    decimal Quantity, decimal PricePerShare, decimal FeesEur, string? Note);

public sealed class EditTradeUseCase(
    IRepositoryBase<Trade> repo, AppDbContext db, ILogger<EditTradeUseCase> log)
    : UseCaseBase<EditTradeInput, Trade>(log)
{
    protected override async Task<Trade> ExecuteCore(EditTradeInput input, CancellationToken ct)
    {
        if (input.Quantity <= 0m)       throw new TradeValidationException("Quantity must be positive.");
        if (input.PricePerShare <= 0m)  throw new TradeValidationException("Price per share must be positive.");

        var existing = await repo.GetByIdAsync(input.Id, ct)
            ?? throw new TradeValidationException($"Trade {input.Id} not found.");

        var updated = existing with
        {
            InstrumentId  = input.InstrumentId,
            ExecutedOn    = input.ExecutedOn,
            Side          = input.Side,
            Quantity      = input.Quantity,
            PricePerShare = input.PricePerShare,
            FeesEur       = input.FeesEur,
            Note          = input.Note,
        };

        // Detach the tracked instance so UpdateAsync can attach the updated copy without conflict.
        db.Entry(existing).State = EntityState.Detached;
        await repo.UpdateAsync(updated, ct);
        return updated;
    }
}
