using TradyStrat.Application.Portfolio;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Portfolio.Events;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Trades.UseCases;

public sealed record DeleteTradeInput(int Id);

public sealed class DeleteTradeUseCase(
    IPortfolioRepository portfolios,
    IClock clock,
    ILogger<DeleteTradeUseCase> log)
    : UseCaseBase<DeleteTradeInput, TradeDeleted>(log)
{
    protected override async Task<TradeDeleted> ExecuteCore(DeleteTradeInput input, CancellationToken ct)
    {
        var portfolio = await portfolios.GetAsync(ct);
        var result = portfolio.DeleteTrade(new TradeId(input.Id), clock.UtcNow());
        await portfolios.SaveAsync(portfolio, ct);
        return result;
    }
}
