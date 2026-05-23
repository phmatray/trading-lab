using Microsoft.AspNetCore.Components;
using TradyStrat.Application.Settings;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.Settings.Anthropic;

namespace TradyStrat.Features.Settings.Components;

public partial class AnthropicSettingsForm : ComponentBase
{
    [Inject] private IAnthropicSettingsRepository AnthropicRepo { get; set; } = default!;
    [Inject] private UpdateAnthropicSettingsUseCase UpdateAnthropic { get; set; } = default!;

    private string _model = "";
    private int _maxTokens = 1500;
    private int _thinkingBudget = 8192;
    private int _maxParallel = 3;
    private string _initialModel = "";
    private int _initialMaxTokens = 1500;
    private int _initialThinkingBudget = 8192;
    private int _initialMaxParallel = 3;
    private string? _msg;
    private bool _isError;
    private bool _busy;
    private DateTime? _lastUpdated;

    protected override async Task OnInitializedAsync()
    {
        var ai = await AnthropicRepo.GetAsync(CancellationToken.None);
        _model = _initialModel = ai.Model.Value;
        _maxTokens = _initialMaxTokens = ai.MaxTokens.Value;
        _thinkingBudget = _initialThinkingBudget = ai.ThinkingBudget.Value;
        _maxParallel = _initialMaxParallel = ai.MaxParallelSuggestions.Value;
        _lastUpdated = await AnthropicRepo.LastUpdatedAsync(CancellationToken.None);
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
            var hasChanges =
                _model != _initialModel
                || _maxTokens != _initialMaxTokens
                || _thinkingBudget != _initialThinkingBudget
                || _maxParallel != _initialMaxParallel;

            if (!hasChanges)
            {
                _msg = "No changes.";
                return;
            }

            var settings = new AnthropicSettings(
                AnthropicModel.Of(_model),
                MaxTokens.Of(_maxTokens),
                ThinkingBudget.Of(_thinkingBudget),
                MaxParallelSuggestions.Of(_maxParallel));

            var ts = await UpdateAnthropic.ExecuteAsync(
                new UpdateAnthropicSettingsInput(settings), CancellationToken.None);

            _initialModel = _model;
            _initialMaxTokens = _maxTokens;
            _initialThinkingBudget = _thinkingBudget;
            _initialMaxParallel = _maxParallel;
            _lastUpdated = ts;
            _msg = "Saved.";
        }
        catch (TradyStratException ex) { _msg = ex.Message; _isError = true; }
        catch (Exception) { _msg = "Save failed — see logs."; _isError = true; }
        finally { _busy = false; }
    }
}
