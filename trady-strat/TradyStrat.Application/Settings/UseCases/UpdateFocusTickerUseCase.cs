using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Settings.Tickers;

namespace TradyStrat.Application.Settings.UseCases;

public sealed record UpdateFocusTickerInput(FocusTicker Ticker);

public sealed class UpdateFocusTickerUseCase(
    IFocusTickerRepository repo,
    IClock clock,
    ILogger<UpdateFocusTickerUseCase> log)
    : UseCaseBase<UpdateFocusTickerInput, DateTime>(log)
{
    protected override async Task<DateTime> ExecuteCore(UpdateFocusTickerInput input, CancellationToken ct)
    {
        await repo.SaveAsync(input.Ticker, ct);
        return clock.UtcNow();
    }
}
