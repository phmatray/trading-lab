using Ardalis.Specification;
using TradyStrat.Application.Abstractions;
using TradyStrat.Shared.Domain;
using TradyStrat.Shared.Exceptions;
using TradyStrat.Shared.Time;

namespace TradyStrat.Application.UseCases.Trades;

public sealed record LogTradeInput(
    DateOnly ExecutedOn, TradeSide Side,
    decimal Quantity, decimal PricePerShare, decimal FeesEur, string? Note);

public sealed class LogTradeUseCase(
    IRepositoryBase<Trade> repo, IClock clock,
    ILogger<LogTradeUseCase> log)
    : UseCaseBase<LogTradeInput, Trade>(log)
{
    protected override async Task<Trade> ExecuteCore(LogTradeInput input, CancellationToken ct)
    {
        if (input.Quantity <= 0m)       throw new TradeValidationException("Quantity must be positive.");
        if (input.PricePerShare <= 0m)  throw new TradeValidationException("Price per share must be positive.");
        if (input.FeesEur < 0m)         throw new TradeValidationException("Fees cannot be negative.");

        var trade = new Trade
        {
            Id = 0,
            ExecutedOn    = input.ExecutedOn,
            Side          = input.Side,
            Quantity      = input.Quantity,
            PricePerShare = input.PricePerShare,
            FeesEur       = input.FeesEur,
            Note          = input.Note,
            CreatedAt     = clock.UtcNow(),
        };
        await repo.AddAsync(trade, ct);
        return trade;
    }
}
