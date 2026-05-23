using TradyStrat.Application.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Settings.Anthropic;

namespace TradyStrat.Application.Settings.UseCases;

public sealed record UpdateAnthropicSettingsInput(AnthropicSettings Settings);

public sealed class UpdateAnthropicSettingsUseCase(
    IAnthropicSettingsRepository repo,
    IClock clock,
    ILogger<UpdateAnthropicSettingsUseCase> log)
    : UseCaseBase<UpdateAnthropicSettingsInput, DateTime>(log)
{
    protected override async Task<DateTime> ExecuteCore(UpdateAnthropicSettingsInput input, CancellationToken ct)
    {
        await repo.SaveAsync(input.Settings, ct);
        return clock.UtcNow();
    }
}
