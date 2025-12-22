using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Pages;

public partial class StrategyOptimization
{
    [Inject] private ICustomStrategyManagementUseCase CustomStrategyUseCase { get; set; } = default!;
    [Inject] private IOptimizeStrategyParametersUseCase OptimizeUseCase { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private ProgressService ProgressService { get; set; } = default!;

    private readonly OptimizationFormModel model = new();
    private List<CustomStrategyResult> customStrategies = new();
    private CustomStrategyResult? selectedStrategy;
    private OptimizationResult? optimizationResult;
    private Domain.ValueObjects.OptimizationProgress? optimizationProgress;

    private bool isLoadingStrategies = true;
    private bool isOptimizing = false;
    private int estimatedIterations = 0;

    protected override async Task OnInitializedAsync()
    {
        await LoadCustomStrategiesAsync();
    }

    private async Task LoadCustomStrategiesAsync()
    {
        isLoadingStrategies = true;

        try
        {
            customStrategies = await CustomStrategyUseCase.GetAllStrategiesAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to load strategies: {ex.Message}");
        }
        finally
        {
            isLoadingStrategies = false;
        }
    }

    private async Task LoadStrategyParameters()
    {
        if (model.CustomStrategyId <= 0)
        {
            selectedStrategy = null;
            model.ParameterRanges.Clear();
            return;
        }

        try
        {
            selectedStrategy = await CustomStrategyUseCase.GetStrategyByIdAsync(model.CustomStrategyId);

            if (selectedStrategy == null)
            {
                return;
            }

            // Extract all unique parameters from entry and exit rules
            model.ParameterRanges.Clear();
            HashSet<string> parameterNames = new();

            foreach (StrategyRule rule in selectedStrategy.Definition.EntryRules.Concat(selectedStrategy.Definition.ExitRules))
            {
                foreach (string paramName in rule.IndicatorParameters.Keys)
                {
                    if (!parameterNames.Contains(paramName))
                    {
                        parameterNames.Add(paramName);

                        // Get current value
                        object currentValue = rule.IndicatorParameters[paramName];
                        decimal currentDecimal = Convert.ToDecimal(currentValue);

                        // Add parameter range with sensible defaults
                        model.ParameterRanges.Add(new ParameterRangeModel
                        {
                            ParameterName = paramName,
                            CurrentValue = currentDecimal,
                            Min = Math.Max(1, currentDecimal * 0.5m),
                            Max = currentDecimal * 1.5m,
                            Step = paramName.ToLower().Contains("period") ? 1m : 0.1m,
                            IsEnabled = true
                        });
                    }
                }
            }

            UpdateEstimatedIterations();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to load strategy: {ex.Message}");
        }
    }

    private void UpdateEstimatedIterations()
    {
        if (model.OptimizationType == OptimizationType.GridSearch)
        {
            // Calculate total combinations
            estimatedIterations = 1;
            foreach (ParameterRangeModel param in model.ParameterRanges.Where(p => p.IsEnabled))
            {
                int steps = (int)Math.Ceiling((param.Max - param.Min) / param.Step) + 1;
                estimatedIterations *= steps;
            }
        }
        else
        {
            // Genetic algorithm iterations
            estimatedIterations = model.PopulationSize * model.Generations;
        }
    }

    private static int CalculateStepCount(ParameterRangeModel param)
    {
        if (param.Step <= 0)
        {
            return 0;
        }

        return (int)Math.Ceiling((param.Max - param.Min) / param.Step) + 1;
    }

    private async Task HandleOptimize()
    {
        if (!model.ParameterRanges.Any(p => p.IsEnabled))
        {
            await ShowWarningAsync("Please enable at least one parameter to optimize");
            return;
        }

        UpdateEstimatedIterations();

        if (estimatedIterations > 10000)
        {
            await ShowWarningAsync($"Optimization will run {estimatedIterations} iterations. This may take a very long time. Consider reducing parameter ranges or using genetic algorithm.");
            return;
        }

        isOptimizing = true;
        optimizationResult = null;
        optimizationProgress = null;

        try
        {
            ProgressService.UpdateProgress("Starting optimization...", 0);

            // Create progress reporter
            Progress<Domain.ValueObjects.OptimizationProgress> progress = new(p =>
            {
                optimizationProgress = p;
                InvokeAsync(() =>
                {
                    ProgressService.UpdateProgress(p.Message, p.PercentComplete);
                    StateHasChanged();
                });
            });

            // Execute optimization
            OptimizeParametersCommand command = model.ToCommand();
            optimizationResult = await OptimizeUseCase.ExecuteAsync(command, progress);

            await ShowSuccessAsync($"Optimization complete! Best score: {optimizationResult.BestScore:F2}");
            ProgressService.Reset();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Optimization failed: {ex.Message}");
            ProgressService.Reset();
        }
        finally
        {
            isOptimizing = false;
        }
    }

    private async Task ApplyBestParameters()
    {
        if (optimizationResult == null || selectedStrategy == null)
        {
            return;
        }

        try
        {
            // Create updated strategy definition with best parameters
            StrategyDefinition updatedDefinition = ApplyParametersToDefinition(
                selectedStrategy.Definition,
                optimizationResult.BestParameters
            );

            // Update strategy
            UpdateCustomStrategyCommand command = new(
                Id: selectedStrategy.Id,
                Name: selectedStrategy.Name,
                Description: selectedStrategy.Description,
                Category: selectedStrategy.Category,
                Definition: updatedDefinition
            );

            await CustomStrategyUseCase.UpdateStrategyAsync(command);

            await ShowSuccessAsync("Strategy updated with best parameters!");

            // Reload strategy
            await LoadStrategyParameters();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to update strategy: {ex.Message}");
        }
    }

    private StrategyDefinition ApplyParametersToDefinition(
        StrategyDefinition baseDefinition,
        Dictionary<string, decimal> parameters)
    {
        // Update entry rules
        List<StrategyRule> updatedEntryRules = baseDefinition.EntryRules
            .Select(rule => ApplyParametersToRule(rule, parameters))
            .ToList();

        // Update exit rules
        List<StrategyRule> updatedExitRules = baseDefinition.ExitRules
            .Select(rule => ApplyParametersToRule(rule, parameters))
            .ToList();

        return new StrategyDefinition(
            EntryRules: updatedEntryRules,
            ExitRules: updatedExitRules,
            SizingMode: baseDefinition.SizingMode,
            SizingParameters: baseDefinition.SizingParameters
        );
    }

    private StrategyRule ApplyParametersToRule(StrategyRule rule, Dictionary<string, decimal> parameters)
    {
        Dictionary<string, object> updatedParams = new(rule.IndicatorParameters);

        foreach ((string key, decimal value) in parameters)
        {
            if (updatedParams.ContainsKey(key))
            {
                // Convert decimal to appropriate type (int for periods, decimal for thresholds)
                object newValue = updatedParams[key] is int
                    ? (int)value
                    : value;

                updatedParams[key] = newValue;
            }
        }

        return rule with { IndicatorParameters = updatedParams };
    }

    private void NavigateToBuilder()
    {
        NavigationManager.NavigateTo("/strategies/builder");
    }

    private void NavigateToBacktest()
    {
        if (selectedStrategy != null)
        {
            NavigationManager.NavigateTo($"/backtest?customStrategyId={selectedStrategy.Id}");
        }
    }

    private async Task ShowSuccessAsync(string message)
    {
        await NotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Success,
            "Success",
            message
        );
    }

    private async Task ShowErrorAsync(string message)
    {
        await NotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Error,
            "Error",
            message
        );
    }

    private async Task ShowWarningAsync(string message)
    {
        await NotificationService.AddNotificationAsync(
            NotificationType.System,
            NotificationSeverity.Warning,
            "Warning",
            message
        );
    }
}
