using TradyStrat.Infrastructure.Settings.UseCases;
using Microsoft.AspNetCore.Components;
using TradyStrat.Application.Goals;
using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Settings;
using TradyStrat.Domain;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Domain.Portfolio;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;

namespace TradyStrat.Features.Settings;

public partial class SettingsPage : ComponentBase
{
    [Inject] private IGoalRepository GoalRepo { get; set; } = default!;
    [Inject] private IPortfolioRepository Portfolios { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private IFocusTickerRepository FocusTickerRepo { get; set; } = default!;
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
        _focusTicker = (await FocusTickerRepo.GetAsync(CancellationToken.None)).Value;
        var existing = await GoalRepo.GetAsync(CancellationToken.None);
        _target = existing.Target.Amount;
        _date   = existing.HasDeadline ? existing.TargetDate.ToDateTime(TimeOnly.MinValue) : null;
        _lastUpdated = existing.UpdatedAt;
        var portfolio = await Portfolios.GetAsync(CancellationToken.None);
        _count = portfolio.Positions.Sum(p => p.Trades.Count);
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
