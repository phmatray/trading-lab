using System.Text.Json;
using Microsoft.AspNetCore.Components;
using TradyStrat.Application.Settings;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Settings.Polymarket;

namespace TradyStrat.Features.Settings.Components;

public partial class PolymarketSettingsForm : ComponentBase
{
    [Inject] private IPolymarketSettingsRepository PolymarketRepo { get; set; } = default!;
    [Inject] private UpdatePolymarketSettingsUseCase UpdatePolymarket { get; set; } = default!;
    // Kept solely for LastUpdatedAsync(Keys) — removed in Phase 6 Task 12 with ISettingsReader.
    [Inject] private ISettingsReader Settings { get; set; } = default!;

    private static readonly string[] Keys =
    [
        SettingsKeys.PolymarketSearchQueries, SettingsKeys.PolymarketMaxMarkets,
        SettingsKeys.PolymarketMinVolumeUsd, SettingsKeys.PolymarketMaxHorizonDays,
    ];

    private string _queriesText = "";
    private int _maxMarkets = 8;
    private decimal _minVolumeUsd = 50_000m;
    private int _maxHorizonDays = 365;

    private string _initialQueriesJson = "[]";
    private int _initialMaxMarkets;
    private decimal _initialMinVolumeUsd;
    private int _initialMaxHorizonDays;

    private string? _msg;
    private bool _isError;
    private bool _busy;
    private DateTime? _lastUpdated;

    protected override async Task OnInitializedAsync()
    {
        var p = await PolymarketRepo.GetAsync(CancellationToken.None);
        _queriesText = string.Join(", ", p.SearchQueries.Values);
        _initialQueriesJson = JsonSerializer.Serialize(p.SearchQueries.Values.ToArray());
        _maxMarkets = _initialMaxMarkets = p.MaxMarkets.Value;
        _minVolumeUsd = _initialMinVolumeUsd = p.MinVolumeUsd.Value;
        _maxHorizonDays = _initialMaxHorizonDays = p.MaxHorizonDays.Value;
        _lastUpdated = await Settings.LastUpdatedAsync(Keys, CancellationToken.None);
    }

    private void OnChanged()
    {
        if (_msg is not null) { _msg = null; _isError = false; }
    }

    private static string[] ParseQueries(string text)
        => text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private async Task SaveAsync()
    {
        if (_busy) return;
        _busy = true; _msg = null; _isError = false;
        try
        {
            var queries = ParseQueries(_queriesText);
            var queriesJson = JsonSerializer.Serialize(queries);

            var hasChanges =
                queriesJson != _initialQueriesJson
                || _maxMarkets != _initialMaxMarkets
                || _minVolumeUsd != _initialMinVolumeUsd
                || _maxHorizonDays != _initialMaxHorizonDays;

            if (!hasChanges)
            {
                _msg = "No changes.";
                return;
            }

            var settings = new PolymarketSettings(
                SearchQueries.Of(queries),
                MaxMarkets.Of(_maxMarkets),
                MinVolumeUsd.Of(_minVolumeUsd),
                MaxHorizonDays.Of(_maxHorizonDays));

            var ts = await UpdatePolymarket.ExecuteAsync(
                new UpdatePolymarketSettingsInput(settings), CancellationToken.None);

            _initialQueriesJson = queriesJson;
            _initialMaxMarkets = _maxMarkets;
            _initialMinVolumeUsd = _minVolumeUsd;
            _initialMaxHorizonDays = _maxHorizonDays;
            _lastUpdated = ts;
            _msg = "Saved.";
        }
        catch (TradyStratException ex) { _msg = ex.Message; _isError = true; }
        catch (Exception) { _msg = "Save failed — see logs."; _isError = true; }
        finally { _busy = false; }
    }
}
