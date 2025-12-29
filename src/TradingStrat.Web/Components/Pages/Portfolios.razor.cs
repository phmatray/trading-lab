using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

public partial class Portfolios : ComponentBase, IDisposable
{
    [Inject] private ICreatePortfolioUseCase CreatePortfolioUseCase { get; set; } = null!;
    [Inject] private IPortfolioPort PortfolioPort { get; set; } = null!;
    [Inject] private PortfolioStateService PortfolioState { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private List<Portfolio>? _portfolios;
    private CreatePortfolioFormModel _createFormModel = new();
    private bool _showCreateDialog = false;
    private bool _showDeleteDialog = false;
    private bool _isLoading = false;
    private bool _isCreating = false;
    private bool _isDeleting = false;
    private int _portfolioIdToDelete;
    private string _portfolioToDelete = string.Empty;
    private string? _errorMessage;
    private string? _successMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadPortfolios();
    }

    private async Task LoadPortfolios()
    {
        _isLoading = true;
        _errorMessage = null;

        try
        {
            _portfolios = await PortfolioPort.GetAllPortfoliosAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading portfolios: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void OpenCreateDialog()
    {
        _createFormModel = new CreatePortfolioFormModel();
        _showCreateDialog = true;
        _errorMessage = null;
        _successMessage = null;
    }

    private void CloseCreateDialog()
    {
        _showCreateDialog = false;
    }

    private async Task HandleCreatePortfolio()
    {
        _errorMessage = null;
        _successMessage = null;
        _isCreating = true;

        try
        {
            var command = new CreatePortfolioCommand(
                _createFormModel.Name,
                _createFormModel.Description,
                _createFormModel.InitialCash
            );

            Result<CreatePortfolioResult> result = await CreatePortfolioUseCase.ExecuteAsync(command);

            if (result.IsSuccess)
            {
                _successMessage = $"Portfolio '{result.Value.Name}' created successfully!";

                await NotificationService.AddNotificationAsync(
                    NotificationType.System,
                    NotificationSeverity.Success,
                    "Portfolio Created",
                    $"'{result.Value.Name}' with ${result.Value.InitialCash:N2} initial cash",
                    metadata: new Dictionary<string, object>
                    {
                        ["portfolioId"] = result.Value.PortfolioId
                    }
                );

                CloseCreateDialog();
                await LoadPortfolios();
            }
            else
            {
                _errorMessage = string.Join(", ", result.Errors.Select(e => e.Message));
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error creating portfolio: {ex.Message}";
        }
        finally
        {
            _isCreating = false;
        }
    }

    private void ConfirmDelete(int portfolioId, string portfolioName, MouseEventArgs e)
    {
        _portfolioIdToDelete = portfolioId;
        _portfolioToDelete = portfolioName ?? "Unknown Portfolio";
        _showDeleteDialog = true;
        _errorMessage = null;
        _successMessage = null;
    }

    private void CloseDeleteDialog()
    {
        _showDeleteDialog = false;
    }

    private async Task HandleDeletePortfolio()
    {
        _errorMessage = null;
        _successMessage = null;
        _isDeleting = true;

        try
        {
            await PortfolioPort.DeletePortfolioAsync(_portfolioIdToDelete);

            _successMessage = $"Portfolio '{_portfolioToDelete}' deleted successfully.";

            await NotificationService.AddNotificationAsync(
                NotificationType.System,
                NotificationSeverity.Info,
                "Portfolio Deleted",
                $"'{_portfolioToDelete}' has been removed"
            );

            CloseDeleteDialog();
            await LoadPortfolios();

            // Clear selected portfolio if it was deleted
            int? selectedId = await PortfolioState.GetSelectedPortfolioIdAsync();
            if (selectedId == _portfolioIdToDelete)
            {
                await PortfolioState.ClearSelectedPortfolioAsync();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error deleting portfolio: {ex.Message}";
        }
        finally
        {
            _isDeleting = false;
        }
    }

    private void NavigateToPortfolio(int portfolioId)
    {
        Navigation.NavigateTo($"/portfolio/{portfolioId}");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
