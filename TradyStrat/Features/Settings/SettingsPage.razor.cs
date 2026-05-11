using Ardalis.Specification;
using Microsoft.AspNetCore.Components;
using TradyStrat.Features.Settings.Config;
using TradyStrat.Features.Settings.UseCases;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;
using TradyStrat.Features.Trades.Specifications;

namespace TradyStrat.Features.Settings;

public partial class SettingsPage : ComponentBase
{
    [Inject] private IReadRepositoryBase<GoalConfig> GoalRepo { get; set; } = default!;
    [Inject] private IReadRepositoryBase<Trade> TradeRepo { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private ISettingsReader Settings { get; set; } = default!;
    [Inject] private UpdateGoalUseCase UpdateGoal { get; set; } = default!;

    private string _focusTicker = "";
    private decimal _target = 1_000_000m;
    private DateTime? _date;
    private string? _msg;
    private bool _isError;
    private bool _busy;
    private int _count;
    private DateTime? _lastUpdated;

    protected override async Task OnInitializedAsync()
    {
        _focusTicker = await Settings.FocusTickerAsync(CancellationToken.None);
        var existing = await GoalRepo.GetByIdAsync(1, CancellationToken.None);
        if (existing is not null)
        {
            _target = existing.TargetEur;
            _date   = existing.TargetDate?.ToDateTime(TimeOnly.MinValue);
            _lastUpdated = existing.UpdatedAt;
        }
        _count = await TradeRepo.CountAsync(new AllTradesSpec(), CancellationToken.None);
    }

    private void OnInputChanged()
    {
        if (_msg is not null) { _msg = null; _isError = false; }
    }

    private async Task SaveAsync()
    {
        if (_busy) return;
        _busy = true;
        _msg = null;
        _isError = false;
        try
        {
            var date = _date is { } d ? DateOnly.FromDateTime(d) : (DateOnly?)null;
            var saved = await UpdateGoal.ExecuteAsync(new UpdateGoalInput(_target, date), CancellationToken.None);
            _lastUpdated = saved.UpdatedAt;
            _msg = "Saved.";
        }
        catch (TradyStratException ex)
        {
            _msg = ex.Message;
            _isError = true;
        }
        catch (Exception)
        {
            _msg = "Save failed — see logs.";
            _isError = true;
        }
        finally
        {
            _busy = false;
        }
    }
}
