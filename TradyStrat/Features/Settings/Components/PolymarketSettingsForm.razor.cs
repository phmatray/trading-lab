using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Features.Settings.Config;
using TradyStrat.Features.Settings.UseCases;

namespace TradyStrat.Features.Settings.Components;

public partial class PolymarketSettingsForm : ComponentBase
{
    [Inject] private ISettingsReader Settings { get; set; } = default!;
    [Inject] private UpdateSettingUseCase UpdateSetting { get; set; } = default!;

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
        var p = await Settings.PolymarketAsync(CancellationToken.None);
        _queriesText = string.Join(", ", p.SearchQueries);
        _initialQueriesJson = JsonSerializer.Serialize(p.SearchQueries.ToArray());
        _maxMarkets = _initialMaxMarkets = p.MaxMarkets;
        _minVolumeUsd = _initialMinVolumeUsd = p.MinVolumeUsd;
        _maxHorizonDays = _initialMaxHorizonDays = p.MaxHorizonDays;
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
            var changed = 0;

            var queries = ParseQueries(_queriesText);
            var queriesJson = JsonSerializer.Serialize(queries);
            if (queriesJson != _initialQueriesJson)
            {
                await UpdateSetting.ExecuteAsync(new UpdateSettingInput(SettingsKeys.PolymarketSearchQueries, queriesJson), CancellationToken.None);
                _initialQueriesJson = queriesJson;
                changed++;
            }
            if (_maxMarkets != _initialMaxMarkets)
            {
                await UpdateSetting.ExecuteAsync(new UpdateSettingInput(SettingsKeys.PolymarketMaxMarkets, _maxMarkets.ToString(CultureInfo.InvariantCulture)), CancellationToken.None);
                _initialMaxMarkets = _maxMarkets; changed++;
            }
            if (_minVolumeUsd != _initialMinVolumeUsd)
            {
                await UpdateSetting.ExecuteAsync(new UpdateSettingInput(SettingsKeys.PolymarketMinVolumeUsd, _minVolumeUsd.ToString(CultureInfo.InvariantCulture)), CancellationToken.None);
                _initialMinVolumeUsd = _minVolumeUsd; changed++;
            }
            if (_maxHorizonDays != _initialMaxHorizonDays)
            {
                await UpdateSetting.ExecuteAsync(new UpdateSettingInput(SettingsKeys.PolymarketMaxHorizonDays, _maxHorizonDays.ToString(CultureInfo.InvariantCulture)), CancellationToken.None);
                _initialMaxHorizonDays = _maxHorizonDays; changed++;
            }

            if (changed > 0) _lastUpdated = await Settings.LastUpdatedAsync(Keys, CancellationToken.None);
            _msg = changed == 0 ? "No changes." : "Saved.";
        }
        catch (TradyStratException ex) { _msg = ex.Message; _isError = true; }
        catch (Exception) { _msg = "Save failed — see logs."; _isError = true; }
        finally { _busy = false; }
    }
}
