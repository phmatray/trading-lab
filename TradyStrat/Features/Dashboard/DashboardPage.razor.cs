using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.Dashboard.Navigation;
using TradyStrat.Features.Dashboard.UseCases;
using TradyStrat.Features.PriceFeed.UseCases;
using TradyStrat.Features.Settings.UseCases;

namespace TradyStrat.Features.Dashboard;

public partial class DashboardPage : ComponentBase, IAsyncDisposable
{
    [Inject] private LoadDashboardUseCase LoadDashboard { get; set; } = default!;
    [Inject] private ForceRefetchSuggestionUseCase ForceRefetch { get; set; } = default!;
    [Inject] private ListInstrumentsUseCase ListInstruments { get; set; } = default!;
    [Inject] private RefreshAllPricesUseCase RefreshPrices { get; set; } = default!;
    [Inject] private IEntryNavigationService Nav { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private IConfiguration Configuration { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "on")] public string? OnParam { get; set; }

    private DashboardViewModel? _vm;
    private string? _error;
    private bool _busy;
    private bool _showRerunConfirm;

    private IJSObjectReference? _keysModule;
    private DotNetObjectReference<DashboardPage>? _selfRef;

    protected override async Task OnParametersSetAsync()
    {
        var ct = CancellationToken.None;

        try
        {
            var result = await OnParamValidator.Validate(OnParam, Nav, ct);

            DateOnly target;
            bool isHistorical;
            switch (result)
            {
                case ValidationResult.RedirectTo r:
                    NavManager.NavigateTo(r.Url, replace: true);
                    return;
                case ValidationResult.Live:
                    target = Clock.TodayInExchangeTzFor(Configuration["Tickers:Focus"]
                        ?? throw new InvalidOperationException("Tickers:Focus is not configured."));
                    isHistorical = false;
                    break;
                case ValidationResult.Historical h:
                    target = h.Date;
                    isHistorical = true;
                    break;
                default:
                    return;
            }

            _vm = await LoadDashboard.ExecuteAsync(
                new LoadDashboardInput(target, isHistorical), ct);
            _error = null;
        }
        catch (TradyStratException ex)
        {
            _vm = null;
            _error = ex.Message;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _keysModule = await JS.InvokeAsync<IJSObjectReference>(
                "import", "./js/dashboard-keys.js");
            _selfRef = DotNetObjectReference.Create(this);
            await _keysModule.InvokeVoidAsync("attach", _selfRef);
        }
    }

    [JSInvokable]
    public Task OnPrev()
    {
        if (_vm?.PrevTradingDay is { } prev)
            NavManager.NavigateTo($"/?on={prev.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        return Task.CompletedTask;
    }

    [JSInvokable]
    public Task OnNext()
    {
        if (_vm?.NextTradingDay is { } next)
            NavManager.NavigateTo($"/?on={next.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
        else if (_vm is { IsHistorical: true })
            NavManager.NavigateTo("/");
        return Task.CompletedTask;
    }

    private void OnDateSelected(DateOnly picked) =>
        NavManager.NavigateTo($"/?on={picked.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");

    private async Task OnRefreshClicked()
    {
        if (_vm?.IsHistorical == true) return;
        _busy = true;
        try   { await RefreshPrices.ExecuteAsync(Common.UseCases.Unit.Value, CancellationToken.None); await ReloadAsync(); }
        finally { _busy = false; }
    }

    private void OnRerunRequested()
    {
        if (_vm?.IsHistorical == true) return;
        _showRerunConfirm = true;
    }

    private async Task ConfirmRerun()
    {
        if (_vm?.IsHistorical == true) return;
        _showRerunConfirm = false;
        _busy = true;
        try
        {
            var ct = CancellationToken.None;
            var focusTicker = _vm?.FocusTicker
                ?? Configuration["Tickers:Focus"]
                ?? throw new InvalidOperationException("Tickers:Focus is not configured.");
            var instruments = await ListInstruments.ExecuteAsync(Common.UseCases.Unit.Value, ct);
            var focus = instruments.SingleOrDefault(i => i.Ticker == focusTicker)
                ?? throw new InvalidOperationException(
                    $"Focus ticker '{focusTicker}' is not in the Instruments table.");
            await ForceRefetch.ExecuteAsync(new ForceRefetchSuggestionInput(focus.Id), ct);
            await ReloadAsync();
        }
        finally { _busy = false; }
    }

    private static void OnLogTradeRequested()
    {
        // Stub — see VaultMasthead nav for /trades.
    }

    // Strip exchange suffixes so the call card reads "100 sh CON3" instead of
    // "100 sh CON3.L". Cheap, deterministic, and keeps the suggestion line
    // visually aligned with the prior single-ticker design.
    private static string FocusLabelFor(string ticker)
    {
        var dot = ticker.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? ticker : ticker[..dot];
    }

    private async Task ReloadAsync()
    {
        if (_vm is null) return;
        _vm = await LoadDashboard.ExecuteAsync(
            new LoadDashboardInput(_vm.Today, _vm.IsHistorical), CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_keysModule is not null)
            {
                await _keysModule.InvokeVoidAsync("detach");
                await _keysModule.DisposeAsync();
            }
        }
        catch (JSDisconnectedException)
        {
            // Circuit gone — nothing to clean up.
        }
        _selfRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}
