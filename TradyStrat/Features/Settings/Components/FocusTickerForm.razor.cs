using Microsoft.AspNetCore.Components;
using TradyStrat.Application.Settings;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Settings.Tickers;

namespace TradyStrat.Features.Settings.Components;

public partial class FocusTickerForm : ComponentBase
{
    [Inject] private IFocusTickerRepository FocusRepo { get; set; } = default!;
    [Inject] private UpdateFocusTickerUseCase UpdateFocus { get; set; } = default!;
    [Inject] private ListInstrumentsUseCase ListInstruments { get; set; } = default!;

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
        var current = await FocusRepo.GetAsync(CancellationToken.None);
        _ticker = _initialTicker = current.Value;
        // If the stored focus isn't among current instruments, show it anyway so the <select> has a value.
        if (!_tickers.Contains(_ticker) && !string.IsNullOrEmpty(_ticker)) _tickers.Insert(0, _ticker);
        _lastUpdated = await FocusRepo.LastUpdatedAsync(CancellationToken.None);
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
            if (_ticker == _initialTicker)
            {
                _msg = "No changes.";
                return;
            }

            var ts = await UpdateFocus.ExecuteAsync(
                new UpdateFocusTickerInput(FocusTicker.Of(_ticker)), CancellationToken.None);
            _initialTicker = _ticker;
            _lastUpdated = ts;
            _msg = "Saved.";
        }
        catch (TradyStratException ex) { _msg = ex.Message; _isError = true; }
        catch (Exception) { _msg = "Save failed — see logs."; _isError = true; }
        finally { _busy = false; }
    }
}
