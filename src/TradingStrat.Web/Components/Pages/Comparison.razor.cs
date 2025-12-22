using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

/// <summary>
/// Code-behind for Comparison page using BaseDataPage pattern.
/// </summary>
public partial class Comparison
{
    private const string FORM_KEY = "comparison-form";

    [Inject] private IParameterOptimizationUseCase ParameterOptimizationUseCase { get; set; } = null!;
    [Inject] private UserPreferencesService PreferencesService { get; set; } = null!;
    [Inject] private IOptions<TradingConfiguration> Configuration { get; set; } = null!;

    private StrategyForm? _strategyFormA;
    private StrategyForm? _strategyFormB;
    private bool _hasRestoredState;

    protected override string FormKey => FORM_KEY;

    protected override async Task<ComparisonFormModel?> InitializeDefaultsAsync()
    {
        // Note: This is called by OnInitializedAsync, but we need OnAfterRenderAsync
        // for strategy form components to be ready. Return null here and handle in OnAfterRenderAsync.
        return null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_hasRestoredState)
        {
            _hasRestoredState = true;

            // Try to restore saved form state
            ComparisonFormModel? savedForm = await FormState.GetFormStateAsync<ComparisonFormModel>(FORM_KEY);
            if (savedForm != null)
            {
                FormModel = savedForm;
            }
            else
            {
                // Initialize from user preferences
                Models.State.UserPreferences prefs = await PreferencesService.GetPreferencesAsync();
                FormModel = ComparisonFormModel.FromPreferences(prefs, Configuration.Value);
            }

            StateHasChanged();
        }
    }

    protected override async Task<ParameterOptimizationResult> ExecuteOperationAsync(
        ComparisonFormModel model,
        IProgress<string> progress)
    {
        // Get current strategy parameters from both forms
        if (_strategyFormA != null)
        {
            model.StrategyParametersA = _strategyFormA.GetCurrentParameters();
        }
        if (_strategyFormB != null)
        {
            model.StrategyParametersB = _strategyFormB.GetCurrentParameters();
        }

        // Convert string progress to OptimizationProgress for the use case
        IProgress<OptimizationProgress> optimizationProgress = new Progress<OptimizationProgress>(p =>
        {
            int percentage = p.TotalBars > 0 ? (int)((double)p.CurrentBar / p.TotalBars * 100) : 0;
            progress.Report($"Testing {p.CurrentVariant}: Bar {p.CurrentBar}/{p.TotalBars} - {p.Trades} trades");

            // Update progress service directly for percentage
            InvokeAsync(() => ProgressService.UpdateProgress(
                $"Testing {p.CurrentVariant}: Bar {p.CurrentBar}/{p.TotalBars} - {p.Trades} trades",
                percentage));
        });

        StrategyVariant variantA = new(
            "Variant A",
            model.StrategyTypeA,
            model.StrategyParametersA,
            GetStrategyDescription(model.StrategyTypeA, model.StrategyParametersA)
        );

        StrategyVariant variantB = new(
            "Variant B",
            model.StrategyTypeB,
            model.StrategyParametersB,
            GetStrategyDescription(model.StrategyTypeB, model.StrategyParametersB)
        );

        ParameterOptimizationCommand command = new(
            model.Ticker,
            variantA,
            variantB,
            model.InitialCapital,
            StartDate: model.StartDate,
            EndDate: model.EndDate
        );

        return await ParameterOptimizationUseCase.ExecuteAsync(command, optimizationProgress);
    }

    protected override string GetSuccessMessage(ParameterOptimizationResult? result)
    {
        if (result == null || result.Comparison == null)
        {
            return "Comparison completed.";
        }

        string winner = result.Comparison.Winner == 1 ? "Variant A" : "Variant B";
        return $"Comparison completed: {winner} wins ({result.ExecutionTime.TotalSeconds:F2}s)";
    }

    private async Task OnFormFieldChanged()
    {
        await FormState.SaveFormStateAsync(FormKey, FormModel);
    }

    private async Task OnStrategyTypeAChanged(string value)
    {
        FormModel.StrategyTypeA = value;
        await OnFormFieldChanged();
    }

    private async Task OnStrategyParametersAChanged(Dictionary<string, object> parameters)
    {
        FormModel.StrategyParametersA = parameters;
        await OnFormFieldChanged();
    }

    private async Task OnStrategyTypeBChanged(string value)
    {
        FormModel.StrategyTypeB = value;
        await OnFormFieldChanged();
    }

    private async Task OnStrategyParametersBChanged(Dictionary<string, object> parameters)
    {
        FormModel.StrategyParametersB = parameters;
        await OnFormFieldChanged();
    }

    private static string GetStrategyDescription(string strategyType, Dictionary<string, object> parameters)
    {
        return strategyType switch
        {
            "ma" => $"MA Crossover ({parameters.GetValueOrDefault("FastPeriod")}/{parameters.GetValueOrDefault("SlowPeriod")})",
            "rsi" => $"RSI ({parameters.GetValueOrDefault("Period")}, {parameters.GetValueOrDefault("OversoldLevel")}/{parameters.GetValueOrDefault("OverboughtLevel")})",
            "macd" => $"MACD ({parameters.GetValueOrDefault("FastPeriod")}/{parameters.GetValueOrDefault("SlowPeriod")}/{parameters.GetValueOrDefault("SignalPeriod")})",
            "ml" => $"ML FastTree ({(decimal)parameters.GetValueOrDefault("BuyThreshold", 0m) * 100:F1}%/{(decimal)parameters.GetValueOrDefault("SellThreshold", 0m) * 100:F1}%)",
            "ichimoku" => $"Ichimoku ({parameters.GetValueOrDefault("TenkanPeriod")}/{parameters.GetValueOrDefault("KijunPeriod")})",
            _ => strategyType
        };
    }

    private static string FormatMetricValue(string metric, decimal value)
    {
        return metric.Contains("Sharpe") || metric.Contains("Factor")
            ? value.ToString("F2")
            : metric.Contains("%") || metric.Contains("Return") || metric.Contains("Drawdown") || metric.Contains("Rate")
                ? $"{value:F2}%"
                : value.ToString("F2");
    }

    private static string GetStrategyType(string strategyName)
    {
        // Map strategy names to types: "Moving Average Crossover" -> "ma"
        return strategyName.ToLowerInvariant() switch
        {
            var s when s.Contains("moving average") => "ma",
            var s when s.Contains("rsi") => "rsi",
            var s when s.Contains("macd") => "macd",
            var s when s.Contains("machine learning") || s.Contains("ml") => "ml",
            var s when s.Contains("ichimoku") => "ichimoku",
            _ => "ma"
        };
    }
}
