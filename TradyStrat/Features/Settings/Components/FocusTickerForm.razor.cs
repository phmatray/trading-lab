using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Application.UseCases;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Application.Settings.UseCases;

namespace TradyStrat.Features.Settings.Components;

public partial class FocusTickerForm : ComponentBase
{
    [Inject] private ISettingsReader Settings { get; set; } = default!;
    [Inject] private ListInstrumentsUseCase ListInstruments { get; set; } = default!;
    [Inject] private UpdateSettingUseCase UpdateSetting { get; set; } = default!;

    private List<string> _tickers = new();
    private string _ticker = "";
    private string _initialTicker = "";
    private string? _msg;
    private bool _isError;
    private bool _busy;
    private DateTime? _lastUpdated;

    protected override async Task OnInitializedAsync()
    {
        var instruments = await ListInstruments.ExecuteAsync(Unit.Value, CancellationToken.None);
        _tickers = instruments.Select(i => i.Ticker).OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
        _ticker = _initialTicker = await Settings.FocusTickerAsync(CancellationToken.None);
        // If the stored focus isn't among current instruments, show it anyway so the <select> has a value.
        if (!_tickers.Contains(_ticker) && !string.IsNullOrEmpty(_ticker)) _tickers.Insert(0, _ticker);
        _lastUpdated = await Settings.LastUpdatedAsync([SettingsKeys.TickersFocus], CancellationToken.None);
    }

    private void OnChanged()
    {
        if (_msg is not null) { _msg = null; _isError = false; }
    }

    private async Task SaveAsync()
    {
        if (_busy) return;
        _busy = true; _msg = null; _isError = false;
        try
        {
            if (_ticker != _initialTicker)
            {
                var ts = await UpdateSetting.ExecuteAsync(new UpdateSettingInput(SettingsKeys.TickersFocus, _ticker), CancellationToken.None);
                _initialTicker = _ticker;
                _lastUpdated = ts;
                _msg = "Saved.";
            }
            else { _msg = "No changes."; }
        }
        catch (TradyStratException ex) { _msg = ex.Message; _isError = true; }
        catch (Exception) { _msg = "Save failed — see logs."; _isError = true; }
        finally { _busy = false; }
    }
}
