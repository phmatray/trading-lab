using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

public partial class DataManagement : ComponentBase, IDisposable
{
    [Inject] private IDataFetchingUseCase DataFetchingUseCase { get; set; } = null!;
    [Inject] private IBulkDataFetchingUseCase BulkDataFetchingUseCase { get; set; } = null!;
    [Inject] private ProgressService ProgressService { get; set; } = null!;
    [Inject] private UserPreferencesService PreferencesService { get; set; } = null!;
    [Inject] private FormStateService FormState { get; set; } = null!;
    [Inject] private IDataFreshnessService DataFreshnessService { get; set; } = null!;
    [Inject] private AppStateService AppState { get; set; } = null!;
    [Inject] private IOptions<TradingConfiguration> Configuration { get; set; } = null!;

    private const string FormKey = "data-fetch-form";
    private const string BulkFormKey = "bulk-fetch-form";
    private const string TimeframeKey = "data-management-timeframe";

    private enum TabType { SingleTicker, BulkFetch }

    private TabType _activeTab = TabType.SingleTicker;
    private TimeFrame _selectedTimeFrame = new() { Unit = TimeFrameUnit.D1 };
    private bool _showCsvDialog;

    // Single ticker state
    private DataFetchFormModel _formModel = new();
    private DataSummaryResult? _result;
    private string? _errorMessage;
    private string? _successMessage;

    // Bulk fetch state
    private BulkFetchFormModel _bulkFormModel = new();
    private BulkFetchResult? _bulkResult;
    private string? _bulkErrorMessage;
    private string? _bulkSuccessMessage;
    private bool _isBulkFetching;
    private List<string> _recentTickers = new();

    private int ParsedTickerCount => ParseTickerList(_bulkFormModel.TickerList ?? "").Count;

    protected override async Task OnInitializedAsync()
    {
        ProgressService.OnProgressChanged += StateHasChanged;

        // Restore selected timeframe from localStorage
        string? savedTimeFrame = await FormState.GetFormStateAsync<string>(TimeframeKey);
        if (savedTimeFrame is not null && Enum.TryParse(savedTimeFrame, out TimeFrameUnit unit))
        {
            _selectedTimeFrame = new TimeFrame { Unit = unit };
        }

        // Restore single ticker form state
        DataFetchFormModel? savedForm = await FormState.GetFormStateAsync<DataFetchFormModel>(FormKey);
        if (savedForm is not null)
        {
            _formModel = savedForm;
        }
        else
        {
            Models.State.UserPreferences prefs = await PreferencesService.GetPreferencesAsync();
            _formModel = DataFetchFormModel.FromPreferences(prefs, Configuration.Value);
        }

        // Restore bulk fetch form state
        BulkFetchFormModel? savedBulkForm = await FormState.GetFormStateAsync<BulkFetchFormModel>(BulkFormKey);
        if (savedBulkForm is not null)
        {
            _bulkFormModel = savedBulkForm;
        }

        // Load recent tickers from app state
        _recentTickers = await AppState.GetRecentTickersAsync();

        // Check data freshness for default ticker
        string ticker = _formModel.Ticker ?? "AAPL";
        await DataFreshnessService.CheckAndNotifyAsync(ticker);
    }

    private async Task HandleTimeFrameChanged(TimeFrame timeFrame)
    {
        _selectedTimeFrame = timeFrame;
        await FormState.SaveFormStateAsync(TimeframeKey, timeFrame.Unit.ToString());
    }

    private string GetTabClasses(TabType tab)
    {
        bool isActive = _activeTab == tab;
        return isActive
            ? "flex items-center border-b-2 border-blue-500 py-4 px-1 text-sm font-medium text-blue-600 dark:text-blue-400"
            : "flex items-center border-b-2 border-transparent py-4 px-1 text-sm font-medium text-gray-500 hover:text-gray-700 hover:border-gray-300 dark:text-gray-400 dark:hover:text-gray-300";
    }

    // Single ticker methods
    private async Task OnFormFieldChanged()
    {
        await FormState.SaveFormStateAsync(FormKey, _formModel);
    }

    private async Task HandleFetchData()
    {
        _errorMessage = null;
        _successMessage = null;
        _result = null;

        await InvokeAsync(() => ProgressService.Reset());

        Progress<string> progress = new(message =>
        {
            InvokeAsync(() => ProgressService.UpdateProgress(message));
        });

        try
        {
            FetchDataCommand command = new(
                _formModel.Ticker,
                _formModel.ISIN,
                _formModel.StartDate,
                _formModel.EndDate
            );

            Result<DataSummaryResult> fetchResult = await DataFetchingUseCase.ExecuteAsync(command, progress);

            if (fetchResult.IsFailure)
            {
                _errorMessage = string.Join(", ", fetchResult.Errors.Select(e => e.Message));
                return;
            }

            _result = fetchResult.Value;
            _successMessage = $"Successfully fetched {_result.NewRecords} new records for {_result.Ticker}";

            // Add to recent tickers
            if (!string.IsNullOrEmpty(_result.Ticker))
            {
                await AppState.AddRecentTickerAsync(_result.Ticker);
                _recentTickers = await AppState.GetRecentTickersAsync();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error fetching data for {_formModel.Ticker}: {ex.Message}";
        }
        finally
        {
            await InvokeAsync(() => ProgressService.Reset());
        }
    }

    // Bulk fetch methods
    private async Task HandleBulkFetchData()
    {
        _bulkErrorMessage = null;
        _bulkSuccessMessage = null;
        _bulkResult = null;
        _isBulkFetching = true;

        await InvokeAsync(() => ProgressService.Reset());

        Progress<BulkFetchProgress> progress = new(p =>
        {
            string message = $"Processing {p.CompletedTickers + 1}/{p.TotalTickers}: {p.CurrentTicker} ({p.ProgressPercentage:F0}%)";
            InvokeAsync(() => ProgressService.UpdateProgress(message, p.ProgressPercentage));
        });

        try
        {
            List<string> tickers = ParseTickerList(_bulkFormModel.TickerList ?? "");

            if (tickers.Count == 0)
            {
                _bulkErrorMessage = "Please enter at least one ticker symbol.";
                return;
            }

            BulkFetchDataCommand command = new(
                Tickers: tickers,
                TimeFrame: _selectedTimeFrame,
                StartDate: _bulkFormModel.StartDate,
                EndDate: _bulkFormModel.EndDate,
                SkipExisting: _bulkFormModel.SkipExisting
            );

            Result<BulkFetchResult> bulkFetchResult = await BulkDataFetchingUseCase.ExecuteAsync(command, progress);

            if (bulkFetchResult.IsFailure)
            {
                _bulkErrorMessage = string.Join(", ", bulkFetchResult.Errors.Select(e => e.Message));
                return;
            }

            _bulkResult = bulkFetchResult.Value;

            _bulkSuccessMessage = $"Completed: {_bulkResult.SuccessfulTickers} successful, " +
                                  $"{_bulkResult.FailedTickers} failed, " +
                                  $"{_bulkResult.SkippedTickers} skipped";

            // Add successful tickers to recent list
            foreach (string ticker in _bulkResult.SuccessfulResults.Keys)
            {
                await AppState.AddRecentTickerAsync(ticker);
            }
            _recentTickers = await AppState.GetRecentTickersAsync();

            // Save form state
            await FormState.SaveFormStateAsync(BulkFormKey, _bulkFormModel);
        }
        catch (Exception ex)
        {
            _bulkErrorMessage = $"Error during bulk fetch: {ex.Message}";
        }
        finally
        {
            _isBulkFetching = false;
            await InvokeAsync(() => ProgressService.Reset());
        }
    }

    private List<string> ParseTickerList(string tickerList)
    {
        if (string.IsNullOrWhiteSpace(tickerList))
        {
            return new List<string>();
        }

        // Split by newlines and commas
        string[] parts = tickerList.Split(new[] { '\r', '\n', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        // Clean up and deduplicate
        HashSet<string> uniqueTickers = new(StringComparer.OrdinalIgnoreCase);
        foreach (string part in parts)
        {
            string ticker = part.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(ticker))
            {
                uniqueTickers.Add(ticker);
            }
        }

        return uniqueTickers.OrderBy(t => t).ToList();
    }

    private void AddTickerToList(string ticker)
    {
        List<string> currentTickers = ParseTickerList(_bulkFormModel.TickerList ?? "");
        if (!currentTickers.Contains(ticker, StringComparer.OrdinalIgnoreCase))
        {
            string newList = string.IsNullOrEmpty(_bulkFormModel.TickerList)
                ? ticker
                : _bulkFormModel.TickerList + "\n" + ticker;
            _bulkFormModel.TickerList = newList;
        }
    }

    private async Task HandleCsvImported(List<string> tickers)
    {
        _bulkFormModel.TickerList = string.Join("\n", tickers);
        await FormState.SaveFormStateAsync(BulkFormKey, _bulkFormModel);
    }

    public void Dispose()
    {
        ProgressService.OnProgressChanged -= StateHasChanged;
    }
}

// Form models
public class BulkFetchFormModel
{
    public string? TickerList { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool SkipExisting { get; set; } = false;
}
