using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

/// <summary>
/// Code-behind for LiveAnalysis page using BaseDataPage pattern.
/// </summary>
public partial class LiveAnalysis
{
    private const string FORM_KEY = "live-analysis-form";

    [Inject] private ILiveAnalysisUseCase LiveAnalysisUseCase { get; set; } = null!;
    [Inject] private UserPreferencesService PreferencesService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private IOptions<TradingConfiguration> Configuration { get; set; } = null!;

    private string? _warningMessage;

    protected override string FormKey => FORM_KEY;

    protected override async Task<AnalysisFormModel?> InitializeDefaultsAsync()
    {
        // Initialize from user preferences
        Models.State.UserPreferences prefs = await PreferencesService.GetPreferencesAsync();
        return AnalysisFormModel.FromPreferences(prefs, Configuration.Value);
    }

    protected override async Task<LiveAnalysisResult> ExecuteOperationAsync(
        AnalysisFormModel model,
        IProgress<string> progress)
    {
        AnalysisCommand command = new(
            model.Ticker,
            model.GetThresholds(),
            model.FetchFreshData
        );

        LiveAnalysisResult result = await LiveAnalysisUseCase.ExecuteAsync(command, progress);

        // Trigger signal notification for Buy/Sell with high confidence
        if (result.PredictedSignal != SignalType.Hold)
        {
            float confidence = Math.Abs(result.PredictedReturn) * 100;
            if (confidence > 70)
            {
                await NotificationService.AddNotificationAsync(
                    NotificationType.Signal,
                    result.PredictedSignal == SignalType.Buy
                        ? NotificationSeverity.Success
                        : NotificationSeverity.Warning,
                    $"{result.PredictedSignal} Signal: {result.Ticker}",
                    $"Price: ${result.CurrentPrice:F2} | Confidence: {confidence:F1}%",
                    ticker: result.Ticker
                );
            }
        }

        // Check data freshness and trigger notification if needed
        if (!result.IsDataFresh && !string.IsNullOrEmpty(result.DataFreshnessWarning))
        {
            _warningMessage = result.DataFreshnessWarning;

            await NotificationService.AddNotificationAsync(
                NotificationType.DataFreshness,
                NotificationSeverity.Warning,
                "Stale Data Warning",
                result.DataFreshnessWarning,
                ticker: result.Ticker,
                action: new NotificationAction
                {
                    Label = "Refresh Data",
                    TargetPage = "/data"
                }
            );
        }

        return result;
    }

    protected override string GetSuccessMessage(LiveAnalysisResult? result)
    {
        if (result == null)
        {
            return "Analysis completed.";
        }

        return $"Analysis completed: {result.PredictedSignal} signal predicted (Return: {result.PredictedReturn * 100:+0.0;-0.0;0.0}%)";
    }

    // Helper for inline Razor event handlers that update properties directly
    private async Task OnFormFieldChanged()
        => await OnPropertyChangedAsync(_ => { }); // No-op update action, just saves state
}
