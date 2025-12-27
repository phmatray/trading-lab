using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Configuration;
using TradingStrat.Application.Factories;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Components.Shared;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services.State;
using TradingStrat.Web.Utilities;

namespace TradingStrat.Web.Components.Pages;

/// <summary>
/// Code-behind for Comparison page using BaseDataPage pattern.
/// </summary>
public partial class Comparison
{
    private const string FORM_KEY = "comparison-form";

    [Inject] private IParameterOptimizationUseCase ParameterOptimizationUseCase { get; set; } = null!;
    [Inject] private IStrategyFactory StrategyFactory { get; set; } = null!;
    [Inject] private Application.Strategies.IStrategyRegistry StrategyRegistry { get; set; } = null!;
    [Inject] private UserPreferencesService PreferencesService { get; set; } = null!;
    [Inject] private IOptions<TradingConfiguration> Configuration { get; set; } = null!;

    private StrategyForm? _strategyFormA;
    private StrategyForm? _strategyFormB;
    private bool _hasRestoredState;

    protected override string FormKey => FORM_KEY;

    protected override Task<ComparisonFormModel?> InitializeDefaultsAsync()
    {
        // Note: This is called by OnInitializedAsync, but we need OnAfterRenderAsync
        // for strategy form components to be ready. Return null here and handle in OnAfterRenderAsync.
        return Task.FromResult<ComparisonFormModel?>(null);
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
                FormModel = ComparisonFormModel.FromPreferences(prefs, Configuration.Value, StrategyRegistry);
            }

            StateHasChanged();
        }
    }

    protected override async Task<Result<ParameterOptimizationResult>> ExecuteOperationAsync(
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
        IProgress<Application.Ports.Inbound.OptimizationProgress> optimizationProgress = new Progress<Application.Ports.Inbound.OptimizationProgress>(p =>
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
            StrategyUIFormatter.GetStrategyDescription(model.StrategyTypeA, model.StrategyParametersA)
        );

        StrategyVariant variantB = new(
            "Variant B",
            model.StrategyTypeB,
            model.StrategyParametersB,
            StrategyUIFormatter.GetStrategyDescription(model.StrategyTypeB, model.StrategyParametersB)
        );

        ParameterOptimizationCommand command = new(
            model.Ticker,
            variantA,
            variantB,
            model.InitialCapital,
            StartDate: model.StartDate,
            EndDate: model.EndDate
        );

        var optimizationResult = await ParameterOptimizationUseCase.ExecuteAsync(command, optimizationProgress);

        if (optimizationResult.IsFailure)
        {
            return Result<ParameterOptimizationResult>.Failure(optimizationResult.Errors);
        }

        return Result<ParameterOptimizationResult>.Success(optimizationResult.Value);
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
        => await OnPropertyChangedAsync(_ => { }); // No-op update action, just saves state

    private async Task OnStrategyTypeAChanged(string value)
    {
        // Parse strategy type string to enum
        if (StrategyRegistry.TryParseStrategyType(value, out var strategyType))
        {
            await OnPropertyChangedAsync(m => m.StrategyTypeA = strategyType);
        }
    }

    private async Task OnStrategyParametersAChanged(Dictionary<string, object> parameters)
        => await OnPropertyChangedAsync(m => m.StrategyParametersA = parameters);

    private async Task OnStrategyTypeBChanged(string value)
    {
        // Parse strategy type string to enum
        if (StrategyRegistry.TryParseStrategyType(value, out var strategyType))
        {
            await OnPropertyChangedAsync(m => m.StrategyTypeB = strategyType);
        }
    }

    private async Task OnStrategyParametersBChanged(Dictionary<string, object> parameters)
        => await OnPropertyChangedAsync(m => m.StrategyParametersB = parameters);

    private string GetStrategyType(string strategyName)
    {
        // Delegate to StrategyFactory for canonical strategy type mapping
        return StrategyFactory.MapStrategyNameToType(strategyName);
    }

    private string GetStrategyKey(StrategyType strategyType)
    {
        // Convert enum to string key for StrategyForm component
        return StrategyRegistry.GetDescriptor(strategyType).Key;
    }
}
