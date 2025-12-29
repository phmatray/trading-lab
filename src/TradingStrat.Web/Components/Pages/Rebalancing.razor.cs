using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

public partial class Rebalancing : ComponentBase, IDisposable
{
    [Inject] private ICalculateRebalancingUseCase CalculateRebalancingUseCase { get; set; } = null!;
    [Inject] private IGetPortfolioSnapshotUseCase GetSnapshotUseCase { get; set; } = null!;
    [Inject] private IPortfolioPort PortfolioPort { get; set; } = null!;
    [Inject] private PortfolioStateService PortfolioState { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ProgressService ProgressService { get; set; } = null!;

    [Parameter]
    public int PortfolioId { get; set; }

    private Portfolio? _portfolio;
    private PortfolioSnapshot? _snapshot;
    private readonly RebalancingFormModel _formModel = new();
    private RebalancingPlan? _plan;
    private bool _isLoading = false;
    private bool _isCalculating = false;
    private string? _errorMessage;
    private string? _warningMessage;

    private List<Shared.BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Portfolios", Href = "/portfolios" },
        new() { Label = "Loading...", Href = "" },
        new() { Label = "Rebalancing", Href = "" }
    };

    protected override async Task OnInitializedAsync()
    {
        ProgressService.OnProgressChanged += StateHasChanged;
        await LoadPortfolioSnapshot();
    }

    protected override async Task OnParametersSetAsync()
    {
        await PortfolioState.SetSelectedPortfolioAsync(PortfolioId);
    }

    private async Task LoadPortfolioSnapshot()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            _portfolio = await PortfolioPort.GetPortfolioByIdAsync(PortfolioId);

            if (_portfolio is null)
            {
                _errorMessage = "Portfolio not found.";
                return;
            }

            await InvokeAsync(() => ProgressService.Reset());

            var progress = new Progress<string>(message =>
            {
                InvokeAsync(() => ProgressService.UpdateProgress(message));
            });

            Result<PortfolioSnapshot> snapshotResult = await GetSnapshotUseCase.ExecuteAsync(PortfolioId, progress);

            if (snapshotResult.IsFailure)
            {
                _errorMessage = $"Failed to load portfolio snapshot: {string.Join(", ", snapshotResult.Errors.Select(e => e.Message))}";
                return;
            }

            _snapshot = snapshotResult.Value;

            // Update breadcrumbs with portfolio name
            if (_portfolio is not null)
            {
                _breadcrumbs = new List<Shared.BreadcrumbNav.Breadcrumb>
                {
                    new() { Label = "Dashboard", Href = "/" },
                    new() { Label = "Portfolios", Href = "/portfolios" },
                    new() { Label = _portfolio.Name, Href = $"/portfolio/{PortfolioId}" },
                    new() { Label = "Rebalancing", Href = $"/portfolio/{PortfolioId}/rebalance" }
                };
            }

            // Initialize form with current positions
            if (_snapshot is not null)
            {
                _formModel.PortfolioId = PortfolioId;
                _formModel.TargetAllocations = _snapshot.Positions
                    .Select(p => new TargetAllocationModel
                    {
                        Ticker = p.Ticker,
                        Percentage = p.AllocationPercentage
                    })
                    .ToList();
                _formModel.CashPercentage = (_snapshot.Cash / _snapshot.TotalValue * 100);
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading portfolio: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            await InvokeAsync(() => ProgressService.Reset());
        }
    }

    private async Task HandleCalculateRebalancing()
    {
        _errorMessage = null;
        _warningMessage = null;
        _plan = null;

        // Validate total allocation
        decimal totalAllocation = GetTotalAllocation();
        if (Math.Abs(totalAllocation - 100m) > 0.01m)
        {
            _errorMessage = $"Total allocation must equal 100%. Current total: {totalAllocation:F1}%";
            return;
        }

        _isCalculating = true;
        await InvokeAsync(() => ProgressService.Reset());

        var progress = new Progress<string>(message =>
        {
            InvokeAsync(() => ProgressService.UpdateProgress(message));
        });

        try
        {
            // Build target weights
            var targetPercentages = _formModel.TargetAllocations
                .Where(a => !string.IsNullOrWhiteSpace(a.Ticker))
                .ToDictionary(
                    a => a.Ticker.ToUpperInvariant(),
                    a => a.Percentage
                );

            var targetWeights = new AllocationWeights(
                targetPercentages,
                _formModel.CashPercentage
            );

            var command = new RebalancingCommand(
                PortfolioId,
                targetWeights,
                _formModel.CommissionPercentage / 100m,
                _formModel.MinimumCommission
            );

            Result<RebalancingPlan> result = await CalculateRebalancingUseCase.ExecuteAsync(command, progress);

            if (result.IsSuccess)
            {
                _plan = result.Value;

                await NotificationService.AddNotificationAsync(
                    NotificationType.System,
                    _plan.IsExecutable ? NotificationSeverity.Success : NotificationSeverity.Warning,
                    "Rebalancing Plan Calculated",
                    $"{_plan.Signals.Count} actions | {(_plan.IsExecutable ? "Executable" : "Insufficient cash")}"
                );
            }
            else
            {
                _errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error calculating rebalancing plan: {ex.Message}";
        }
        finally
        {
            _isCalculating = false;
            await InvokeAsync(() => ProgressService.Reset());
        }
    }

    private decimal GetTotalAllocation()
    {
        decimal positionsTotal = _formModel.TargetAllocations
            .Where(a => !string.IsNullOrWhiteSpace(a.Ticker))
            .Sum(a => a.Percentage);
        return positionsTotal + _formModel.CashPercentage;
    }

    private void AddAllocation()
    {
        _formModel.TargetAllocations.Add(new TargetAllocationModel());
    }

    private void RemoveAllocation(TargetAllocationModel allocation)
    {
        _formModel.TargetAllocations.Remove(allocation);
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
