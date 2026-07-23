using TradyStrat.Domain.Settings.Tickers;

namespace TradyStrat.Application.Settings;

public interface IFocusTickerRepository
{
    Task<FocusTicker> GetAsync(CancellationToken ct);

    /// <summary>Throws <see cref="TradyStrat.Domain.Settings.SettingValidationException"/>
    /// if the ticker doesn't match any registered Instrument.</summary>
    Task SaveAsync(FocusTicker ticker, CancellationToken ct);

    /// <summary>UpdatedAt of the focus-ticker row, or null if it doesn't exist. Used by the Settings form to show "last saved at".</summary>
    Task<DateTime?> LastUpdatedAsync(CancellationToken ct);
}
