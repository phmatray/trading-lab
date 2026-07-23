using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using AppStateService = TradingStrat.Web.Services.State.AppStateService;

namespace TradingStrat.Web.Components.Pages;

public partial class StrategyOptimization : IDisposable
{
    [Inject] private ICustomStrategyManagementUseCase CustomStrategyUseCase { get; set; } = default!;
    [Inject] private IOptimizeStrategyParametersUseCase OptimizeUseCase { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;
    [Inject] private NotificationService NotificationService { get; set; } = default!;
    [Inject] private ProgressService ProgressService { get; set; } = default!;
    [Inject] private AppStateService AppState { get; set; } = default!;

    private readonly OptimizationFormModel _model = new();
    private List<CustomStrategyResult> _customStrategies = new();
    private CustomStrategyResult? _selectedStrategy;
    private OptimizationResult? _optimizationResult;
    private Domain.ValueObjects.OptimizationProgress? _optimizationProgress;

    private bool _isLoadingStrategies = true;
    private bool _isOptimizing;
    private int _estimatedIterations;

    private readonly List<Shared.BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Strategy Library", Href = "/strategies/library" },
        new() { Label = "Strategy Optimization", Href = "/strategies/optimize" }
    };

    protected override async Task OnInitializedAsync()
    {
        ProgressService.OnProgressChanged += StateHasChanged;
        await LoadCustomStrategiesAsync();
    }

    private async Task LoadCustomStrategiesAsync()
    {
        _isLoadingStrategies = true;

        try
        {
            Result<List<CustomStrategyResult>> result = await CustomStrategyUseCase.GetAllStrategiesAsync();

            if (result.IsFailure)
            {
                await ShowErrorAsync(string.Join(", ", result.Errors.Select(e => e.Message)));
                _customStrategies = new List<CustomStrategyResult>();
                return;
            }

            _customStrategies = result.Value;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to load strategies: {ex.Message}");
        }
        finally
        {
            _isLoadingStrategies = false;
        }
    }

    private async Task LoadStrategyParameters()
    {
        if (_model.CustomStrategyId <= 0)
        {
            _selectedStrategy = null;
            _model.ParameterRanges.Clear();
            return;
        }

        try
        {
            Result<CustomStrategyResult> result = await CustomStrategyUseCase.GetStrategyByIdAsync(_model.CustomStrategyId);

            if (result.IsFailure)
            {
                await ShowErrorAsync(string.Join(", ", result.Errors.Select(e => e.Message)));
                _selectedStrategy = null;
                return;
            }

            _selectedStrategy = result.Value;

            if (_selectedStrategy is null || _selectedStrategy.Definition is null)
            {
                if (_selectedStrategy?.Definition is null)
                {
                    await ShowErrorAsync("Strategy optimization is only available for rule-based strategies");
                }
                return;
            }

            // Extract all unique parameters from entry and exit rules
            _model.ParameterRanges.Clear();
            HashSet<string> parameterNames = new();

            foreach (StrategyRule rule in _selectedStrategy.Definition.EntryRules.Concat(_selectedStrategy.Definition.ExitRules))
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
                        _model.ParameterRanges.Add(new ParameterRangeModel
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
        if (_model.OptimizationType == OptimizationType.GridSearch)
        {
            // Calculate total combinations
            _estimatedIterations = 1;
            foreach (ParameterRangeModel param in _model.ParameterRanges.Where(p => p.IsEnabled))
            {
                int steps = (int)Math.Ceiling((param.Max - param.Min) / param.Step) + 1;
                _estimatedIterations *= steps;
            }
        }
        else
        {
            // Genetic algorithm iterations
            _estimatedIterations = _model.PopulationSize * _model.Generations;
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
        if (!_model.ParameterRanges.Any(p => p.IsEnabled))
        {
            await ShowWarningAsync("Please enable at least one parameter to optimize");
            return;
        }

        UpdateEstimatedIterations();

        if (_estimatedIterations > 10000)
        {
            await ShowWarningAsync($"Optimization will run {_estimatedIterations} iterations. This may take a very long time. Consider reducing parameter ranges or using genetic algorithm.");
            return;
        }

        _isOptimizing = true;
        _optimizationResult = null;
        _optimizationProgress = null;

        try
        {
            await InvokeAsync(() => ProgressService.Reset());
            await InvokeAsync(() => ProgressService.UpdateProgress("Starting optimization...", 0));

            // Create progress reporter
            Progress<Domain.ValueObjects.OptimizationProgress> progress = new(p =>
            {
                _optimizationProgress = p;
                InvokeAsync(() =>
                {
                    ProgressService.UpdateProgress(p.Message, p.PercentComplete);
                    StateHasChanged();
                });
            });

            // Execute optimization
            OptimizeParametersCommand command = _model.ToCommand();
            Result<OptimizationResult> result = await OptimizeUseCase.ExecuteAsync(command, progress);

            if (result.IsFailure)
            {
                await ShowErrorAsync(string.Join(", ", result.Errors.Select(e => e.Message)));
                return;
            }

            _optimizationResult = result.Value;

            await ShowSuccessAsync($"Optimization complete! Best score: {_optimizationResult.BestScore:F2}");

            // Save optimization context for quick actions
            var optimizationContext = new Models.State.OptimizationContext
            {
                CustomStrategyId = _model.CustomStrategyId,
                BestParameters = _optimizationResult.BestParameters,
                BestObjectiveValue = _optimizationResult.BestScore,
                OptimizationAlgorithm = _model.OptimizationType.ToString()
            };
            await AppState.SetOptimizationContextAsync(optimizationContext);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Optimization failed: {ex.Message}");
        }
        finally
        {
            await InvokeAsync(() => ProgressService.Reset());
            _isOptimizing = false;
        }
    }

    private async Task ApplyBestParameters()
    {
        if (_optimizationResult is null || _selectedStrategy is null || _selectedStrategy.Definition is null)
        {
            return;
        }

        try
        {
            // Create updated strategy definition with best parameters
            StrategyDefinition updatedDefinition = ApplyParametersToDefinition(
                _selectedStrategy.Definition,
                _optimizationResult.BestParameters
            );

            // Update strategy
            UpdateCustomStrategyCommand command = new(
                Id: _selectedStrategy.Id,
                Name: _selectedStrategy.Name,
                Description: _selectedStrategy.Description,
                Category: _selectedStrategy.Category,
                Definition: updatedDefinition
            );

            Result<CustomStrategyResult> updateResult = await CustomStrategyUseCase.UpdateStrategyAsync(command);

            if (updateResult.IsFailure)
            {
                await ShowErrorAsync(string.Join(", ", updateResult.Errors.Select(e => e.Message)));
                return;
            }

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
            entryRules: updatedEntryRules,
            exitRules: updatedExitRules,
            sizingMode: baseDefinition.SizingMode,
            sizingParameters: baseDefinition.SizingParameters
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

        return new StrategyRule(
            rule.IndicatorName,
            updatedParams,
            rule.Operator,
            rule.ValueType,
            rule.ConstantValue,
            rule.SecondIndicatorName,
            rule.SecondIndicatorParameters,
            rule.LogicalOperator);
    }

    private void NavigateToBuilder()
    {
        NavigationManager.NavigateTo("/strategies/builder");
    }

    private void NavigateToBacktest()
    {
        if (_selectedStrategy is not null)
        {
            NavigationManager.NavigateTo($"/backtest?customStrategyId={_selectedStrategy.Id}");
        }
    }

    // Quick Actions navigation methods
    private void CreatePortfolioFromStrategy()
    {
        if (_optimizationResult is null || _selectedStrategy is null)
        {
            return;
        }

        // Navigate to portfolios page (optimization context already saved via AppState)
        NavigationManager.NavigateTo("/portfolios");
    }

    private void CompareVariations()
    {
        if (_optimizationResult is null || _selectedStrategy is null)
        {
            return;
        }

        // Navigate to strategy comparison page
        NavigationManager.NavigateTo("/strategies/compare");
    }

    private async Task SaveAsNewStrategy()
    {
        if (_optimizationResult is null || _selectedStrategy is null || _selectedStrategy.Definition is null)
        {
            return;
        }

        try
        {
            // Create updated strategy definition with best parameters
            StrategyDefinition optimizedDefinition = ApplyParametersToDefinition(
                _selectedStrategy.Definition,
                _optimizationResult.BestParameters
            );

            // Create new strategy with optimized parameters
            CreateCustomStrategyCommand command = new(
                Name: $"{_selectedStrategy.Name} (Optimized)",
                Description: $"{_selectedStrategy.Description} - Optimized with {_model.Objective} objective. Best score: {_optimizationResult.BestScore:F2}",
                Category: _selectedStrategy.Category,
                Author: _selectedStrategy.Author,
                Definition: optimizedDefinition
            );

            Result<CustomStrategyResult> createResult = await CustomStrategyUseCase.CreateStrategyAsync(command);

            if (createResult.IsFailure)
            {
                await ShowErrorAsync(string.Join(", ", createResult.Errors.Select(e => e.Message)));
                return;
            }

            CustomStrategyResult newStrategy = createResult.Value;

            await ShowSuccessAsync($"New strategy '{newStrategy.Name}' created successfully!");

            // Reload strategies list
            await LoadCustomStrategiesAsync();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to create new strategy: {ex.Message}");
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

    public void Dispose()
    {
        ProgressService.OnProgressChanged -= StateHasChanged;
        GC.SuppressFinalize(this);
    }
}
