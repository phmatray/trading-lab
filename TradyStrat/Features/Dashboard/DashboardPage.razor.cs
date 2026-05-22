using TradyStrat.Domain.Suggestions;
using TradyStrat.Infrastructure.PriceFeed.UseCases;
using TradyStrat.Application.Dashboard;
using System.Globalization;
using Ardalis.Specification;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using TradyStrat.Domain.Exceptions;
using TradyStrat.Domain;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.AiSuggestion.Backfill;
using TradyStrat.Application.AiSuggestion.Specifications;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Dashboard.Navigation;
using TradyStrat.Application.Dashboard.UseCases;
using TradyStrat.Application.Settings.Config;
using TradyStrat.Application.Settings.UseCases;

namespace TradyStrat.Features.Dashboard;

public partial class DashboardPage : ComponentBase, IAsyncDisposable
{
    [Inject] private LoadDashboardUseCase LoadDashboard { get; set; } = default!;
    [Inject] private ForceRefetchSuggestionUseCase ForceRefetch { get; set; } = default!;
    [Inject] private ListInstrumentsUseCase ListInstruments { get; set; } = default!;
    [Inject] private RefreshAllPricesUseCase RefreshPrices { get; set; } = default!;
    [Inject] private StreamTodaysSuggestionsUseCase StreamSuggestions { get; set; } = default!;
    [Inject] private BuildFocusDerivedSliceUseCase BuildFocusSlice { get; set; } = default!;
    [Inject] private ISuggestionBackfillCoordinator BackfillCoord { get; set; } = default!;
    [Inject] private IReadRepositoryBase<Suggestion> SuggestionRepo { get; set; } = default!;
    [Inject] private IEntryNavigationService Nav { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private ISettingsReader Settings { get; set; } = default!;
    [Inject] private NavigationManager NavManager { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ILogger<DashboardPage> Log { get; set; } = default!;

    [SupplyParameterFromQuery(Name = "on")] public string? OnParam { get; set; }

    private DashboardViewModel? _vm;
    private string? _error;
    private bool _busy;
    private bool _showRerunConfirm;

    // Live-mode mutable state mutated by the stream consumer in Phase 9.
    private SuggestionState? _focusState;
    private Dictionary<int, SuggestionState?> _tickerStates = new();
    private FocusDerivedSlice _focusDerived = FocusDerivedSlice.Empty;
    private CancellationTokenSource? _streamCts;

    private IJSObjectReference? _keysModule;
    private IJSObjectReference? _stickyModule;
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
                    target = Clock.TodayInExchangeTzFor(await Settings.FocusTickerAsync(ct));
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

            // Mirror the skeleton's state into mutable page fields. The stream consumer
            // (Phase 9) mutates these per arrival.
            _focusState = _vm.FocusCallState;
            _tickerStates.Clear();
            foreach (var t in _vm.Tickers)
                _tickerStates[t.InstrumentId] = t.CallState;
            _focusDerived = new FocusDerivedSlice(_vm.CallDiff, _vm.IndicatorHistories, _vm.MarketSnapshot);

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

            if (_vm is { IsHistorical: false })
            {
                _streamCts?.Cancel();
                _streamCts?.Dispose();
                _streamCts = new CancellationTokenSource();
                _ = ConsumeStreamAsync(_streamCts.Token);
            }
        }

        if (_vm is not null)
        {
            _stickyModule ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./js/sticky-bar.js");
            await _stickyModule.InvokeVoidAsync("observeHero");
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
        try   { await RefreshPrices.ExecuteAsync(Application.UseCases.Unit.Value, CancellationToken.None); await ReloadAsync(); }
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
                ?? await Settings.FocusTickerAsync(ct);
            var instruments = await ListInstruments.ExecuteAsync(Application.UseCases.Unit.Value, ct);
            var focus = instruments.SingleOrDefault(i => i.Ticker == focusTicker)
                ?? throw new InvalidOperationException(
                    $"Focus ticker '{focusTicker}' is not in the Instruments table.");
            await ForceRefetch.ExecuteAsync(new ForceRefetchSuggestionInput(focus.Id), ct);
            await ReloadAsync();

            // Restart the stream — explicit cancel/restart so an in-flight stream from
            // the initial load is replaced.
            _streamCts?.Cancel();
            _streamCts?.Dispose();
            _streamCts = new CancellationTokenSource();
            _ = ConsumeStreamAsync(_streamCts.Token);
        }
        finally { _busy = false; }
    }

    private static void OnLogTradeRequested()
    {
        // Stub — see VaultMasthead nav for /trades.
    }

    /// <summary>
    /// User-driven retry from the failed TodaysCallCard. Despite being wired to
    /// the focus card, this restarts the entire stream — every held ticker still
    /// in <c>Pending</c> is re-enumerated and re-attempted, not just the focus.
    /// The focus state is flipped back to Pending so the skeleton reappears
    /// immediately while the AI call is re-issued via GetTodaysSuggestionUseCase.
    /// </summary>
    private async Task OnRetryStream()
    {
        if (_vm is null) return;
        var focus = _vm.Tickers.FirstOrDefault(t => t.Ticker == _vm.FocusTicker);
        if (focus is null) return;

        _focusState = new SuggestionState.Pending();
        _tickerStates[focus.InstrumentId] = new SuggestionState.Pending();
        await InvokeAsync(StateHasChanged);

        _streamCts?.Cancel();
        _streamCts?.Dispose();
        _streamCts = new CancellationTokenSource();
        _ = ConsumeStreamAsync(_streamCts.Token);
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

        _focusState = _vm.FocusCallState;
        _tickerStates.Clear();
        foreach (var t in _vm.Tickers)
            _tickerStates[t.InstrumentId] = t.CallState;
        _focusDerived = new FocusDerivedSlice(_vm.CallDiff, _vm.IndicatorHistories, _vm.MarketSnapshot);
    }

    private async Task ConsumeStreamAsync(CancellationToken ct)
    {
        if (_vm is null) return;

        var heldIds = _vm.Tickers
            .Where(t => t.CallState is SuggestionState.Pending)
            .Select(t => t.InstrumentId)
            .ToArray();
        if (heldIds.Length == 0) return;

        var focusInstrumentId = _vm.Tickers
            .FirstOrDefault(t => t.Ticker == _vm.FocusTicker)?.InstrumentId ?? -1;

        try
        {
            await foreach (var ev in StreamSuggestions.StreamAsync(heldIds, ct))
            {
                SuggestionState newState = ev switch
                {
                    SuggestionStreamEvent.Ready r  => new SuggestionState.Ready(r.Suggestion),
                    SuggestionStreamEvent.Failed f => new SuggestionState.Failed(f.Reason),
                    _ => throw new InvalidOperationException($"Unknown event type: {ev.GetType()}"),
                };

                _tickerStates[ev.InstrumentId] = newState;

                if (ev.InstrumentId == focusInstrumentId)
                {
                    _focusState = newState;
                    if (newState is SuggestionState.Ready ready)
                    {
                        _focusDerived = await BuildFocusSlice.BuildAsync(ready.Suggestion, _vm.Today, ct);
                        await KickBackfillChain(ready.Suggestion);
                    }
                }

                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested — silent.
        }
    }

    private async Task KickBackfillChain(Suggestion focus)
    {
        if (_vm is null || _vm.IsHistorical) return;
        var today = _vm.Today;

        // Replicates the original LoadDashboardUseCase guard: read the prior
        // suggestion and skip when none exists or when the gap is < 1 day.
        var prior = await SuggestionRepo.FirstOrDefaultAsync(
            new PriorSuggestionSpec(today, focus.InstrumentId), CancellationToken.None);

        if (prior is not { ForDate: var lastDate } || today.AddDays(-1) <= lastDate)
            return;

        _ = BackfillCoord
            .EnsureBackfilledAsync(lastDate, today.AddDays(-1), CancellationToken.None)
            .ContinueWith(
                t => DashboardPageLog.BackfillCrashed(Log, t.Exception),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }

    public async ValueTask DisposeAsync()
    {
        try { _streamCts?.Cancel(); }
        catch (ObjectDisposedException) { }
        _streamCts?.Dispose();
        _streamCts = null;

        if (_stickyModule is not null)
        {
            try
            {
                await _stickyModule.InvokeVoidAsync("disconnect");
                await _stickyModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Circuit gone — module already torn down.
            }
        }

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

internal static partial class DashboardPageLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Backfill chain crashed unobserved")]
    public static partial void BackfillCrashed(ILogger logger, Exception? ex);
}
