using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

public partial class PerformanceAnalytics : ComponentBase, IDisposable
{
    [Inject] private IGetPortfolioPerformanceUseCase GetPerformanceUseCase { get; set; } = null!;
    [Inject] private IPortfolioPort PortfolioPort { get; set; } = null!;
    [Inject] private PortfolioStateService PortfolioState { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ProgressService ProgressService { get; set; } = null!;

    [Parameter]
    public int PortfolioId { get; set; }

    private Portfolio? _portfolio;
    private PortfolioPerformanceHistory? _performanceHistory;
    private DateTime _startDate = DateTime.Today.AddYears(-1);
    private DateTime _endDate = DateTime.Today;
    private bool _isLoading = false;
    private string? _errorMessage;

    private List<Shared.BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Portfolios", Href = "/portfolios" },
        new() { Label = "Loading...", Href = "" },
        new() { Label = "Performance", Href = "" }
    };

    protected override async Task OnInitializedAsync()
    {
        ProgressService.OnProgressChanged += StateHasChanged;
        await LoadPortfolio();
        await HandleAnalyze();
    }

    protected override async Task OnParametersSetAsync()
    {
        await PortfolioState.SetSelectedPortfolioAsync(PortfolioId);
    }

    private async Task LoadPortfolio()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            _portfolio = await PortfolioPort.GetPortfolioByIdAsync(PortfolioId);

            if (_portfolio == null)
            {
                _errorMessage = "Portfolio not found.";
            }
            else
            {
                // Update breadcrumbs with portfolio name
                _breadcrumbs = new List<Shared.BreadcrumbNav.Breadcrumb>
                {
                    new() { Label = "Dashboard", Href = "/" },
                    new() { Label = "Portfolios", Href = "/portfolios" },
                    new() { Label = _portfolio.Name, Href = $"/portfolio/{PortfolioId}" },
                    new() { Label = "Performance", Href = $"/portfolio/{PortfolioId}/performance" }
                };
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading portfolio: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task HandleAnalyze()
    {
        _errorMessage = null;
        _performanceHistory = null;

        if (_portfolio == null)
        {
            return;
        }

        await InvokeAsync(() => ProgressService.Reset());

        var progress = new Progress<string>(message =>
        {
            InvokeAsync(() => ProgressService.UpdateProgress(message));
        });

        try
        {
            var query = new PortfolioPerformanceQuery(
                PortfolioId,
                _startDate,
                _endDate
            );

            Result<PortfolioPerformanceHistory> result = await GetPerformanceUseCase.ExecuteAsync(query, progress);

            if (result.IsSuccess)
            {
                _performanceHistory = result.Value;

                await NotificationService.AddNotificationAsync(
                    NotificationType.System,
                    NotificationSeverity.Success,
                    "Performance Analysis Complete",
                    $"{_performanceHistory.DataPoints.Count} data points | {_performanceHistory.CurrentMetrics.TotalReturnPercentage:+0.0;-0.0;0.0}% return"
                );
            }
            else
            {
                _errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error analyzing performance: {ex.Message}";
        }
        finally
        {
            await InvokeAsync(() => ProgressService.Reset());
        }
    }

    private void SetQuickRange(int months)
    {
        _endDate = DateTime.Today;
        _startDate = DateTime.Today.AddMonths(-months);
    }

    private void SetQuickRangeAll()
    {
        _endDate = DateTime.Today;
        _startDate = _portfolio?.CreatedAt.Date ?? DateTime.Today.AddYears(-10);
    }

    private void NavigateBack()
    {
        Navigation.NavigateTo($"/portfolio/{PortfolioId}");
    }

    public void Dispose()
    {
        ProgressService.OnProgressChanged -= StateHasChanged;
    }
}
