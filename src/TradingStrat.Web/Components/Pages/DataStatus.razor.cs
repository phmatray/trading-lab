using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Web.Components.Base;

namespace TradingStrat.Web.Components.Pages;

public partial class DataStatus : BaseComponent
{
    [Inject] private IGetAllDataStatusUseCase GetAllDataStatusUseCase { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private AllDataStatusResult? _dataStatus;
    private bool _isLoading = true;
    private string? _errorMessage;

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
            _dataStatus = await GetAllDataStatusUseCase.ExecuteAsync();
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

    private void NavigateToDataManagement()
    {
        Navigation.NavigateTo("/data");
    }
}
