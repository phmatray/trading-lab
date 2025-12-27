using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Web.Components.Base;

namespace TradingStrat.Web.Components.Pages;

public partial class Dashboard : BaseComponent, IDisposable
{
    [Inject] private IGetDashboardStatsUseCase GetDashboardStatsUseCase { get; set; } = null!;
    [Inject] private IGetRecentActivityUseCase GetRecentActivityUseCase { get; set; } = null!;
    [Inject] private IGetTopStrategiesUseCase GetTopStrategiesUseCase { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private DashboardStatsResult? _stats;
    private List<ActivityEvent> _recentActivity = new();
    private List<TopStrategyResult> _topStrategies = new();
    private bool _isLoading = true;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboardDataAsync();
    }

    private async Task LoadDashboardDataAsync()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            // Load all dashboard data in parallel
            var statsTask = GetDashboardStatsUseCase.ExecuteAsync();
            var activityTask = GetRecentActivityUseCase.ExecuteAsync(limit: 10);
            var strategiesTask = GetTopStrategiesUseCase.ExecuteAsync(limit: 5);

            await Task.WhenAll(statsTask, activityTask, strategiesTask);

            var statsResult = await statsTask;
            var activityResult = await activityTask;
            var strategiesResult = await strategiesTask;

            if (statsResult.IsFailure)
            {
                _errorMessage = string.Join(", ", statsResult.Errors.Select(e => e.Message));
                return;
            }

            if (activityResult.IsFailure)
            {
                _errorMessage = string.Join(", ", activityResult.Errors.Select(e => e.Message));
                return;
            }

            if (strategiesResult.IsFailure)
            {
                _errorMessage = string.Join(", ", strategiesResult.Errors.Select(e => e.Message));
                return;
            }

            _stats = statsResult.Value;
            _recentActivity = activityResult.Value;
            _topStrategies = strategiesResult.Value;
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Failed to load dashboard data");
            _errorMessage = "Unable to load dashboard. Please try again.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void NavigateToPage(string path)
    {
        Navigation.NavigateTo(path);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
