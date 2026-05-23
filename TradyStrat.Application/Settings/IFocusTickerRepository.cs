using TradyStrat.Domain.Settings.Tickers;

namespace TradyStrat.Application.Settings;

public interface IFocusTickerRepository
{
    Task<FocusTicker> GetAsync(CancellationToken ct);

    /// <summary>Throws <see cref="TradyStrat.Domain.Exceptions.SettingValidationException"/>
    /// if the ticker doesn't match any registered Instrument.</summary>
    Task SaveAsync(FocusTicker ticker, CancellationToken ct);
}
