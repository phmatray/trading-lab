using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Settings.Polymarket;

namespace TradyStrat.Application.Settings.UseCases;

public sealed record UpdatePolymarketSettingsInput(PolymarketSettings Settings);

public sealed class UpdatePolymarketSettingsUseCase(
    IPolymarketSettingsRepository repo,
    IClock clock,
    ILogger<UpdatePolymarketSettingsUseCase> log)
    : UseCaseBase<UpdatePolymarketSettingsInput, DateTime>(log)
{
    protected override async Task<DateTime> ExecuteCore(UpdatePolymarketSettingsInput input, CancellationToken ct)
    {
        await repo.SaveAsync(input.Settings, ct);
        return clock.UtcNow();
    }
}
