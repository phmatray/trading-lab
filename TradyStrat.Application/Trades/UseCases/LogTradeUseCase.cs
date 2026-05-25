using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Portfolio.Events;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Application.Trades.UseCases;

public sealed record LogTradeInput(
    int InstrumentId, DateOnly ExecutedOn, TradeSide Side,
    decimal Quantity, decimal PricePerShare, decimal FeesEur, string? Note);

public sealed class LogTradeUseCase(
    IPortfolioRepository portfolios,
    IInstrumentRepository instruments,
    IClock clock,
    ILogger<LogTradeUseCase> log)
    : UseCaseBase<LogTradeInput, TradeRecorded>(log)
{
    protected override async Task<TradeRecorded> ExecuteCore(LogTradeInput input, CancellationToken ct)
    {
        var instrument = await instruments.GetAsync(new InstrumentId(input.InstrumentId), ct)
            ?? throw new InstrumentNotFoundException($"Instrument {input.InstrumentId} not found.");
        var portfolio = await portfolios.GetAsync(ct);

        var quantity = Quantity.Of(input.Quantity);
        var price    = Price.Of(Money.Of(input.PricePerShare, instrument.Currency));
        var fees     = Money.Of(input.FeesEur, Currency.Eur);

        var result = portfolio.RecordTrade(
            instrument.Id,
            input.ExecutedOn, input.Side,
            quantity, price, fees,
            input.Note ?? "",
            clock.UtcNow());

        await portfolios.SaveAsync(portfolio, ct);
        return result;
    }
}
