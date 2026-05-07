using Microsoft.AspNetCore.Components;
using TradyStrat.Application.Abstractions;
using TradyStrat.Application.UseCases.AiSuggestion;
using TradyStrat.Application.UseCases.Dashboard;
using TradyStrat.Application.UseCases.Prices;
using TradyStrat.Shared.Exceptions;

namespace TradyStrat.Features.Dashboard;

public partial class DashboardPage : ComponentBase
{
    [Inject] private LoadDashboardUseCase LoadDashboard { get; set; } = default!;
    [Inject] private ForceRefetchSuggestionUseCase ForceRefetch { get; set; } = default!;
    [Inject] private RefreshAllPricesUseCase RefreshPrices { get; set; } = default!;

    private DashboardViewModel? _vm;
    private string? _error;
    private bool _busy;
    private bool _showRerunConfirm;

    protected override async Task OnInitializedAsync() => await Reload();

    private async Task Reload()
    {
        try
        {
            _vm = await LoadDashboard.ExecuteAsync(Unit.Value, CancellationToken.None);
            _error = null;
        }
        catch (TradyStratException ex)
        {
            _vm = null;
            _error = ex.Message;
        }
    }

    private async Task OnRefreshClicked()
    {
        _busy = true;
        try   { await RefreshPrices.ExecuteAsync(Unit.Value, CancellationToken.None); await Reload(); }
        finally { _busy = false; }
    }

    private void OnRerunRequested() => _showRerunConfirm = true;

    private async Task ConfirmRerun()
    {
        _showRerunConfirm = false;
        _busy = true;
        try   { await ForceRefetch.ExecuteAsync(Unit.Value, CancellationToken.None); await Reload(); }
        finally { _busy = false; }
    }

    private static void OnLogTradeRequested()
    {
        // Phase 7 wires this to /trades navigation or an inline dialog.
    }
}
