using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Web.Components.Base;

namespace TradingStrat.Web.Components.Pages;

public partial class BacktestArchive : BaseComponent
{
    [Inject] private IGetBacktestArchiveUseCase GetBacktestArchiveUseCase { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private BacktestArchiveResult? _archiveResult;
    private bool _isLoading;
    private string? _errorMessage;

    private readonly List<Shared.BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Backtest Archive", Href = "/backtests" }
    };

    // Filter state
    private string? _filterTicker;
    private string? _filterStrategy;
    private DateTime? _filterStartDate;
    private DateTime? _filterEndDate;
    private string _sortBy = "ExecutedAt";
    private bool _sortDescending = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadArchiveAsync();
    }

    private async Task LoadArchiveAsync()
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            StateHasChanged();

            var query = new GetBacktestArchiveQuery(
                Ticker: _filterTicker,
                StrategyType: _filterStrategy,
                StartDate: _filterStartDate,
                EndDate: _filterEndDate,
                SortBy: _sortBy,
                SortDescending: _sortDescending,
                Limit: 100
            );

            _archiveResult = await GetBacktestArchiveUseCase.ExecuteAsync(query);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load backtest archive");
            _errorMessage = "Failed to load backtest archive. Please try again.";
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    private async Task ApplyFiltersAsync()
    {
        await LoadArchiveAsync();
    }

    private async Task ClearFiltersAsync()
    {
        _filterTicker = null;
        _filterStrategy = null;
        _filterStartDate = null;
        _filterEndDate = null;
        _sortBy = "ExecutedAt";
        _sortDescending = true;
        await LoadArchiveAsync();
    }

    private async Task ChangeSortAsync(string sortBy)
    {
        if (_sortBy == sortBy)
        {
            // Toggle sort direction
            _sortDescending = !_sortDescending;
        }
        else
        {
            _sortBy = sortBy;
            _sortDescending = true;
        }
        await LoadArchiveAsync();
    }

    private void NavigateToBacktest()
    {
        Navigation.NavigateTo("/backtest");
    }

    private void ViewBacktestDetails(int backtestId)
    {
        // Navigate to backtest page with run ID to reload the backtest
        Navigation.NavigateTo($"/backtest?runId={backtestId}");
    }

    private string GetSortIcon(string columnName)
    {
        if (_sortBy != columnName)
        {
            return "";
        }

        return _sortDescending ? "↓" : "↑";
    }

    private string GetStatusBadgeClass(string status)
    {
        return status switch
        {
            "Success" => "bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400",
            "Failed" => "bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-400",
            "Cancelled" => "bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400",
            _ => "bg-gray-100 text-gray-800 dark:bg-gray-900/30 dark:text-gray-400"
        };
    }

    private string GetPerformanceClass(decimal? value)
    {
        if (!value.HasValue || value.Value == 0)
        {
            return "text-gray-600 dark:text-dark-text-secondary";
        }

        return value.Value > 0
            ? "text-green-600 dark:text-green-400"
            : "text-red-600 dark:text-red-400";
    }
}
