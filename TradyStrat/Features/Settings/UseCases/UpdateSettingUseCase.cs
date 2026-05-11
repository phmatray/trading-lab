using Ardalis.Specification;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Settings.Config;
using TradyStrat.Features.Settings.Specifications;

namespace TradyStrat.Features.Settings.UseCases;

public sealed record UpdateSettingInput(string Key, string RawValue);

public sealed class UpdateSettingUseCase(
    ISettingsRegistry registry,
    IReadRepositoryBase<Instrument> instruments,
    ISettingsService settings,
    ILogger<UpdateSettingUseCase> log)
    : UseCaseBase<UpdateSettingInput, DateTime>(log)
{
    protected override async Task<DateTime> ExecuteCore(UpdateSettingInput input, CancellationToken ct)
    {
        var descriptor = registry.Get(input.Key);   // InvalidOperationException for an unknown key

        object parsed;
        try { parsed = descriptor.Parse(input.RawValue); }
        catch (Exception ex) { throw new SettingValidationException($"'{input.Key}' value is not valid.", ex); }

        descriptor.Validate?.Invoke(parsed);         // throws SettingValidationException

        if (input.Key == SettingsKeys.TickersFocus)
        {
            var ticker = (string)parsed;
            var known = await instruments.AnyAsync(new InstrumentByTickerSpec(ticker), ct);
            if (!known) throw new SettingValidationException($"No instrument with ticker '{ticker}'.");
        }

        var raw = descriptor.Format?.Invoke(parsed) ?? input.RawValue;
        await settings.SetAsync(input.Key, raw, ct);
        return (await settings.LastUpdatedAsync([input.Key], ct))!.Value;   // never null right after SetAsync
    }
}
