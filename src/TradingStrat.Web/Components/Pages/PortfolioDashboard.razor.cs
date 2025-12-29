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

public partial class PortfolioDashboard : ComponentBase, IDisposable
{
    [Inject] private IGetPortfolioSnapshotUseCase GetSnapshotUseCase { get; set; } = null!;
    [Inject] private IPortfolioPort PortfolioPort { get; set; } = null!;
    [Inject] private IManagePositionsUseCase ManagePositionsUseCase { get; set; } = null!;
    [Inject] private PortfolioStateService PortfolioState { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ProgressService ProgressService { get; set; } = null!;

    [Parameter]
    public int PortfolioId { get; set; }

    private Portfolio? _portfolio;
    private PortfolioSnapshot? _snapshot;
    private AddPositionFormModel _addPositionFormModel = new();
    private bool _showAddPositionDialog;
    private bool _isLoading;
    private bool _isAddingPosition;
    private string? _errorMessage;
    private string? _successMessage;

    private List<Shared.BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Portfolios", Href = "/portfolios" },
        new() { Label = "Loading...", Href = "" }
    };

    protected override async Task OnInitializedAsync()
    {
        ProgressService.OnProgressChanged += StateHasChanged;
        await LoadPortfolioSnapshot();
    }

    protected override async Task OnParametersSetAsync()
    {
        // Set the selected portfolio in state when navigating to this page
        await PortfolioState.SetSelectedPortfolioAsync(PortfolioId);
    }

    private async Task LoadPortfolioSnapshot()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            // Load basic portfolio info
            _portfolio = await PortfolioPort.GetPortfolioByIdAsync(PortfolioId);

            if (_portfolio is null)
            {
                _errorMessage = "Portfolio not found.";
                return;
            }

            await InvokeAsync(() => ProgressService.Reset());

            // Get snapshot with current market prices
            var progress = new Progress<string>(message =>
            {
                InvokeAsync(async () =>
                {
                    ProgressService.UpdateProgress(message);
                    await InvokeAsync(StateHasChanged);
                });
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
                    new() { Label = _portfolio.Name, Href = $"/portfolio/{PortfolioId}" }
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
            await InvokeAsync(() => ProgressService.Reset());
        }
    }

    private async Task RefreshPrices()
    {
        _errorMessage = null;
        _successMessage = null;

        await InvokeAsync(() => ProgressService.Reset());

        var progress = new Progress<string>(message =>
        {
            InvokeAsync(() => ProgressService.UpdateProgress(message));
        });

        try
        {
            Result<PortfolioSnapshot> snapshotResult = await GetSnapshotUseCase.ExecuteAsync(PortfolioId, progress);

            if (snapshotResult.IsFailure)
            {
                _errorMessage = $"Failed to refresh prices: {string.Join(", ", snapshotResult.Errors.Select(e => e.Message))}";
                return;
            }

            _snapshot = snapshotResult.Value;
            _successMessage = "Prices refreshed successfully.";

            await NotificationService.AddNotificationAsync(
                NotificationType.System,
                NotificationSeverity.Success,
                "Prices Refreshed",
                $"Portfolio '{_snapshot.PortfolioName}' updated with latest market prices"
            );
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error refreshing prices: {ex.Message}";
        }
        finally
        {
            await InvokeAsync(() => ProgressService.Reset());
        }
    }

    private void OpenAddPositionDialog()
    {
        _addPositionFormModel = new AddPositionFormModel
        {
            PortfolioId = PortfolioId,
            EntryDate = DateTime.Today
        };
        _showAddPositionDialog = true;
        _errorMessage = null;
        _successMessage = null;
    }

    private void CloseAddPositionDialog()
    {
        _showAddPositionDialog = false;
    }

    private async Task HandleAddPosition()
    {
        _errorMessage = null;
        _successMessage = null;
        _isAddingPosition = true;

        try
        {
            _addPositionFormModel.NormalizeTicker();

            var command = new AddPositionCommand(
                _addPositionFormModel.PortfolioId,
                _addPositionFormModel.Ticker,
                _addPositionFormModel.Quantity,
                _addPositionFormModel.EntryPrice,
                _addPositionFormModel.EntryDate,
                _addPositionFormModel.Notes
            );

            Result<Position> result = await ManagePositionsUseCase.AddPositionAsync(command);

            if (result.IsSuccess)
            {
                Position position = result.Value;
                decimal costBasis = position.Quantity * position.EntryPrice;
                _successMessage = $"Position added: {position.Quantity} shares of {position.Ticker} at {position.EntryPrice:C2}";

                await NotificationService.AddNotificationAsync(
                    NotificationType.System,
                    NotificationSeverity.Success,
                    "Position Added",
                    $"{position.Quantity} shares of {position.Ticker} (${costBasis:N2})",
                    ticker: position.Ticker
                );

                CloseAddPositionDialog();
                await LoadPortfolioSnapshot();
            }
            else
            {
                _errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error adding position: {ex.Message}";
        }
        finally
        {
            _isAddingPosition = false;
        }
    }

    private void NavigateBack()
    {
        Navigation.NavigateTo("/portfolios");
    }

    public void Dispose()
    {
        ProgressService.OnProgressChanged -= StateHasChanged;
    }
}
