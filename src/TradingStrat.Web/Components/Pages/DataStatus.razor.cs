using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Components.Base;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Pages;

public partial class DataStatus : BaseComponent
{
    [Inject] private IDataStatusCacheService CacheService { get; set; } = null!;
    [Inject] private IBulkDataFetchingUseCase BulkDataFetchingUseCase { get; set; } = null!;
    [Inject] private IDeleteHistoricalDataUseCase DeleteHistoricalDataUseCase { get; set; } = null!;
    [Inject] private IExportHistoricalDataUseCase ExportHistoricalDataUseCase { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private DataStatusQuery _query = new() { TimeFrame = new TimeFrame { Unit = TimeFrameUnit.D1 } };
    private AllDataStatusResult? _dataStatus;
    private bool _isLoading = true;
    private string? _errorMessage;
    private readonly HashSet<string> _selectedTickers = new();

    private readonly List<Shared.BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Data Status", Href = "/data/status" }
    };

    private bool _isAllSelected =>
        _dataStatus?.TickerStatuses.Any() == true &&
        _dataStatus.TickerStatuses.All(s => _selectedTickers.Contains(s.Ticker));

    protected override async Task OnInitializedAsync()
    {
        await LoadDataStatusAsync();
    }

    private async Task LoadDataStatusAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            var result = await CacheService.GetOrFetchDataStatusAsync(_query);

            if (result.IsFailure)
            {
                _errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
                await HandleErrorAsync(new Exception(_errorMessage), "Failed to load data status");
            }
            else
            {
                _dataStatus = result.Value;
            }
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load data status");
            _errorMessage = "Unable to load data status. Please try again.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    // Event handlers
    private async Task HandleTimeFrameChanged(TimeFrame timeFrame)
    {
        _query = _query with { TimeFrame = timeFrame, PageNumber = 1 };
        await LoadDataStatusAsync();
    }

    private async Task HandleSearchChanged(string? searchTerm)
    {
        _query = _query with { SearchTicker = searchTerm, PageNumber = 1 };
        await LoadDataStatusAsync();
    }

    private async Task HandleFilterChanged(FilterPanel.FilterValues filters)
    {
        _query = _query with
        {
            StatusFilter = filters.StatusFilter,
            MinCoverage = filters.MinCoverage,
            MaxCoverage = filters.MaxCoverage,
            PageNumber = 1
        };
        await LoadDataStatusAsync();
    }

    private async Task HandleFilterReset()
    {
        _query = _query with
        {
            StatusFilter = null,
            MinCoverage = null,
            MaxCoverage = null,
            PageNumber = 1
        };
        await LoadDataStatusAsync();
    }

    private async Task HandleSort((SortColumn Column, SortDirection Direction) sort)
    {
        _query = _query with { SortBy = sort.Column, SortDirection = sort.Direction };
        await LoadDataStatusAsync();
    }

    private async Task HandlePageChanged(int newPage)
    {
        _query = _query with { PageNumber = newPage };
        await LoadDataStatusAsync();
    }

    private async Task HandlePageSizeChanged(int newPageSize)
    {
        _query = _query with { PageSize = newPageSize, PageNumber = 1 };
        await LoadDataStatusAsync();
    }

    // Selection methods
    private bool IsSelected(string ticker) => _selectedTickers.Contains(ticker);

    private void HandleSelectAll(ChangeEventArgs e)
    {
        bool isChecked = (bool)(e.Value ?? false);

        if (isChecked && _dataStatus != null)
        {
            foreach (TickerDataStatus status in _dataStatus.TickerStatuses)
            {
                _selectedTickers.Add(status.Ticker);
            }
        }
        else
        {
            _selectedTickers.Clear();
        }
    }

    private void HandleRowSelectionChanged(string ticker, ChangeEventArgs e)
    {
        bool isChecked = (bool)(e.Value ?? false);

        if (isChecked)
        {
            _selectedTickers.Add(ticker);
        }
        else
        {
            _selectedTickers.Remove(ticker);
        }
    }

    private void ClearSelection()
    {
        _selectedTickers.Clear();
    }

    // Action methods
    private async Task RefreshTicker(string ticker)
    {
        try
        {
            if (_query.TimeFrame == null)
            {
                await NotificationService.ShowErrorAsync("Please select a timeframe");
                return;
            }

            BulkFetchDataCommand command = new(
                Tickers: new List<string> { ticker },
                TimeFrame: _query.TimeFrame,
                StartDate: null,
                EndDate: null,
                SkipExisting: false
            );

            var bulkResult = await BulkDataFetchingUseCase.ExecuteAsync(command);

            if (bulkResult.IsFailure)
            {
                await NotificationService.ShowErrorAsync($"Failed to refresh {ticker}: {string.Join(", ", bulkResult.Errors.Select(e => e.Message))}");
                return;
            }

            BulkFetchResult result = bulkResult.Value;

            if (result.SuccessfulTickers > 0)
            {
                await NotificationService.ShowSuccessAsync($"Refreshed {ticker} successfully");
                CacheService.InvalidateCache();
                await LoadDataStatusAsync();
            }
            else if (result.FailedResults.ContainsKey(ticker))
            {
                await NotificationService.ShowErrorAsync($"Failed to refresh {ticker}: {result.FailedResults[ticker]}");
            }
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync($"Error refreshing {ticker}: {ex.Message}");
        }
    }

    private async Task DeleteTicker(string ticker)
    {
        if (!await NotificationService.ConfirmAsync($"Delete all data for {ticker}?", "This action cannot be undone."))
        {
            return;
        }

        try
        {
            if (_query.TimeFrame == null)
            {
                await NotificationService.ShowErrorAsync("Please select a timeframe");
                return;
            }

            var deleteResult = await DeleteHistoricalDataUseCase.DeleteTickerAsync(ticker, _query.TimeFrame);

            if (deleteResult.IsFailure)
            {
                await NotificationService.ShowErrorAsync($"Failed to delete {ticker}: {string.Join(", ", deleteResult.Errors.Select(e => e.Message))}");
                return;
            }

            DeleteDataResult result = deleteResult.Value;

            await NotificationService.ShowSuccessAsync($"Deleted {result.RecordsDeleted} records for {ticker}");
            CacheService.InvalidateCache();
            await LoadDataStatusAsync();
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync($"Error deleting {ticker}: {ex.Message}");
        }
    }

    private async Task RefreshSelectedTickers()
    {
        if (!_selectedTickers.Any())
        {
            return;
        }

        try
        {
            if (_query.TimeFrame == null)
            {
                await NotificationService.ShowErrorAsync("Please select a timeframe");
                return;
            }

            BulkFetchDataCommand command = new(
                Tickers: _selectedTickers.ToList(),
                TimeFrame: _query.TimeFrame,
                StartDate: null,
                EndDate: null,
                SkipExisting: false
            );

            var bulkResult = await BulkDataFetchingUseCase.ExecuteAsync(command);

            if (bulkResult.IsFailure)
            {
                await NotificationService.ShowErrorAsync($"Failed to refresh tickers: {string.Join(", ", bulkResult.Errors.Select(e => e.Message))}");
                return;
            }

            BulkFetchResult result = bulkResult.Value;

            await NotificationService.ShowSuccessAsync($"Refreshed {result.SuccessfulTickers} tickers successfully");

            if (result.FailedTickers > 0)
            {
                await NotificationService.ShowWarningAsync($"{result.FailedTickers} tickers failed to refresh");
            }

            CacheService.InvalidateCache();
            await LoadDataStatusAsync();
            ClearSelection();
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync($"Error refreshing selected tickers: {ex.Message}");
        }
    }

    private async Task DeleteSelectedTickers()
    {
        if (!_selectedTickers.Any())
        {
            return;
        }

        if (!await NotificationService.ConfirmAsync(
            $"Delete data for {_selectedTickers.Count} tickers?",
            "This action cannot be undone."))
        {
            return;
        }

        try
        {
            if (_query.TimeFrame == null)
            {
                await NotificationService.ShowErrorAsync("Please select a timeframe");
                return;
            }

            int totalDeleted = 0;
            foreach (string ticker in _selectedTickers)
            {
                var deleteResult = await DeleteHistoricalDataUseCase.DeleteTickerAsync(ticker, _query.TimeFrame);

                if (deleteResult.IsFailure)
                {
                    await NotificationService.ShowErrorAsync($"Failed to delete {ticker}: {string.Join(", ", deleteResult.Errors.Select(e => e.Message))}");
                    continue;
                }

                totalDeleted += deleteResult.Value.RecordsDeleted;
            }

            await NotificationService.ShowSuccessAsync($"Deleted {totalDeleted} records for {_selectedTickers.Count} tickers");
            CacheService.InvalidateCache();
            await LoadDataStatusAsync();
            ClearSelection();
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync($"Error deleting selected tickers: {ex.Message}");
        }
    }

    private async Task ExportSelectedTickers()
    {
        if (!_selectedTickers.Any())
        {
            return;
        }

        try
        {
            if (_query.TimeFrame == null)
            {
                await NotificationService.ShowErrorAsync("Please select a timeframe");
                return;
            }

            foreach (string ticker in _selectedTickers)
            {
                string outputPath = Path.Combine(Path.GetTempPath(), $"{ticker}_{_query.TimeFrame.Unit}.csv");
                ExportResult result = await ExportHistoricalDataUseCase.ExportHistoricalDataAsync(
                    ticker,
                    _query.TimeFrame,
                    ExportFormat.CSV,
                    outputPath
                );

                // In a real implementation, this would trigger a download
                await NotificationService.ShowInfoAsync($"Exported {ticker} to {result.FilePath}");
            }

            await NotificationService.ShowSuccessAsync($"Exported {_selectedTickers.Count} tickers");
            ClearSelection();
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync($"Error exporting selected tickers: {ex.Message}");
        }
    }

    private async Task ExportCoverageReport()
    {
        try
        {
            if (_query.TimeFrame == null)
            {
                await NotificationService.ShowErrorAsync("Please select a timeframe");
                return;
            }

            string outputPath = Path.Combine(Path.GetTempPath(), $"coverage_report_{_query.TimeFrame.Unit}.csv");
            ExportResult result = await ExportHistoricalDataUseCase.ExportCoverageReportAsync(_query.TimeFrame, outputPath);

            await NotificationService.ShowSuccessAsync($"Coverage report exported to {result.FilePath}");
        }
        catch (Exception ex)
        {
            await NotificationService.ShowErrorAsync($"Error exporting coverage report: {ex.Message}");
        }
    }

    private void NavigateToDataManagement()
    {
        Navigation.NavigateTo("/data");
    }

    // UI helper methods
    private static string GetCoverageColorClass(decimal coverage)
    {
        return coverage switch
        {
            >= 95 => "text-green-600 dark:text-green-400 font-semibold",
            >= 80 => "text-yellow-600 dark:text-yellow-400 font-semibold",
            _ => "text-red-600 dark:text-red-400 font-semibold"
        };
    }

    private static string GetStatusBadgeClass(decimal coverage)
    {
        return coverage switch
        {
            >= 95 => "px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300",
            >= 80 => "px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300",
            _ => "px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300"
        };
    }

    private static string GetStatusText(decimal coverage)
    {
        return coverage switch
        {
            >= 95 => "Complete",
            >= 80 => "Partial",
            _ => "Gaps"
        };
    }
}
