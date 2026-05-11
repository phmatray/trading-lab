using System.Globalization;
using Microsoft.AspNetCore.Components;
using TradyStrat.Common.Exceptions;
using TradyStrat.Features.Settings.Config;
using TradyStrat.Features.Settings.UseCases;

namespace TradyStrat.Features.Settings.Components;

public partial class AnthropicSettingsForm : ComponentBase
{
    [Inject] private ISettingsReader Settings { get; set; } = default!;
    [Inject] private UpdateSettingUseCase UpdateSetting { get; set; } = default!;

    private static readonly string[] Keys = [SettingsKeys.AnthropicModel, SettingsKeys.AnthropicMaxTokens];

    private string _model = "";
    private int _maxTokens = 1500;
    private string _initialModel = "";
    private int _initialMaxTokens = 1500;
    private string? _msg;
    private bool _isError;
    private bool _busy;
    private DateTime? _lastUpdated;

    protected override async Task OnInitializedAsync()
    {
        var ai = await Settings.AnthropicAsync(CancellationToken.None);
        _model = _initialModel = ai.Model;
        _maxTokens = _initialMaxTokens = ai.MaxTokens;
        _lastUpdated = await Settings.LastUpdatedAsync(Keys, CancellationToken.None);
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
            var changed = 0;
            if (_model != _initialModel)
            {
                await UpdateSetting.ExecuteAsync(new UpdateSettingInput(SettingsKeys.AnthropicModel, _model), CancellationToken.None);
                _initialModel = _model;
                changed++;
            }
            if (_maxTokens != _initialMaxTokens)
            {
                await UpdateSetting.ExecuteAsync(new UpdateSettingInput(SettingsKeys.AnthropicMaxTokens, _maxTokens.ToString(CultureInfo.InvariantCulture)), CancellationToken.None);
                _initialMaxTokens = _maxTokens;
                changed++;
            }
            if (changed > 0) _lastUpdated = await Settings.LastUpdatedAsync(Keys, CancellationToken.None);
            _msg = changed == 0 ? "No changes." : "Saved.";
        }
        catch (TradyStratException ex) { _msg = ex.Message; _isError = true; }
        catch (Exception) { _msg = "Save failed — see logs."; _isError = true; }
        finally { _busy = false; }
    }
}
