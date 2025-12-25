using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

public partial class DataManagement : ComponentBase, IDisposable
{
    [Inject] private IDataFetchingUseCase DataFetchingUseCase { get; set; } = null!;
    [Inject] private ProgressService ProgressService { get; set; } = null!;
    [Inject] private UserPreferencesService PreferencesService { get; set; } = null!;
    [Inject] private FormStateService FormState { get; set; } = null!;
    [Inject] private IDataFreshnessService DataFreshnessService { get; set; } = null!;
    [Inject] private IOptions<TradingConfiguration> Configuration { get; set; } = null!;

    private const string FORM_KEY = "data-fetch-form";

    private DataFetchFormModel _formModel = new();
    private DataSummaryResult? _result;
    private string? _errorMessage;
    private string? _successMessage;

    protected override async Task OnInitializedAsync()
    {
        ProgressService.OnProgressChanged += StateHasChanged;

        // Try to restore saved form state
        DataFetchFormModel? savedForm = await FormState.GetFormStateAsync<DataFetchFormModel>(FORM_KEY);
        if (savedForm != null)
        {
            _formModel = savedForm;
        }
        else
        {
            // Initialize from user preferences
            Models.State.UserPreferences prefs = await PreferencesService.GetPreferencesAsync();
            _formModel = DataFetchFormModel.FromPreferences(prefs, Configuration.Value);
        }

        // Check data freshness for default ticker
        string ticker = _formModel.Ticker ?? "AAPL";
        await DataFreshnessService.CheckAndNotifyAsync(ticker);
    }

    private async Task OnFormFieldChanged()
    {
        await FormState.SaveFormStateAsync(FORM_KEY, _formModel);
    }

    private async Task HandleFetchData()
    {
        _errorMessage = null;
        _successMessage = null;
        _result = null;

        await InvokeAsync(() => ProgressService.Reset());

        var progress = new Progress<string>(message =>
        {
            InvokeAsync(() => ProgressService.UpdateProgress(message));
        });

        try
        {
            var command = new FetchDataCommand(
                _formModel.Ticker,
                _formModel.ISIN,
                _formModel.StartDate,
                _formModel.EndDate
            );

            _result = await DataFetchingUseCase.ExecuteAsync(command, progress);
            _successMessage = $"Successfully fetched {_result.NewRecords} new records for {_result.Ticker}";
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error fetching data: {ex.Message}";
        }
        finally
        {
            await InvokeAsync(() => ProgressService.Reset());
        }
    }

    public void Dispose()
    {
        ProgressService.OnProgressChanged -= StateHasChanged;
    }
}
