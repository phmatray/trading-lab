using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;
using static TradingStrat.Web.Services.DebugLogger;

namespace TradingStrat.Web.Components.Pages;

public partial class StrategyBuilder
{
    [Parameter]
    public int? Id { get; set; }

    [Inject]
    private ICustomStrategyManagementUseCase CustomStrategyUseCase { get; set; } = null!;

    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    [Inject]
    private NotificationService NotificationService { get; set; } = null!;

    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    private bool IsEditMode => Id.HasValue;

    private readonly StrategyFormModel _formModel = new()
    {
        StrategyType = CustomStrategyType.RuleBased // Default to RuleBased
    };
    private bool _isLoading;
    private bool _isSaving;
    private string _loadingMessage = "Loading...";
    private readonly List<string> _validationErrors = [];

    // Python strategy fields
    private bool _isValidating;
    private bool _isDryRunning;
    private readonly List<string> _pythonValidationErrors = [];
    private DryRunResult? _dryRunResult;

    private List<Shared.BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Strategy Library", Href = "/strategies/library" },
        new() { Label = "Strategy Builder", Href = "/strategies/builder" }
    };

    private const string DefaultPythonCode = @"import talib
import numpy as np

# Global variables to store pre-calculated indicators
sma_20 = None
sma_50 = None

def initialize(prices):
    """"""
    Optional: Pre-calculate indicators once when strategy is loaded.
    This is more efficient than calculating on every bar.

    Args:
        prices: Dictionary with NumPy arrays
            - prices[""close""], prices[""open""], prices[""high""]
            - prices[""low""], prices[""volume""], prices[""dates""]
    """"""
    global sma_20, sma_50

    # Calculate moving averages once
    sma_20 = talib.SMA(prices[""close""], timeperiod=20)
    sma_50 = talib.SMA(prices[""close""], timeperiod=50)

def generate_signal(index, price, cash, position):
    """"""
    Required: Generate trading signal for the current bar.

    Args:
        index: Current bar index (0-based)
        price: Current closing price
        cash: Available cash
        position: Current shares held

    Returns:
        Dictionary with:
            - ""action"": ""buy"", ""sell"", or ""hold""
            - ""quantity"": Number of shares to trade (integer)
            - ""reason"": String explaining the signal
    """"""
    # Wait for enough data
    if index < 50:
        return {""action"": ""hold"", ""quantity"": 0, ""reason"": ""Insufficient data""}

    # Golden cross: SMA20 crosses above SMA50 (bullish signal)
    if sma_20[index-1] <= sma_50[index-1] and sma_20[index] > sma_50[index] and position == 0:
        # Buy with 95% of available cash
        quantity = int((cash * 0.95) / price)
        return {""action"": ""buy"", ""quantity"": quantity, ""reason"": f""Golden cross: SMA20 ({sma_20[index]:.2f}) > SMA50 ({sma_50[index]:.2f})""}

    # Death cross: SMA20 crosses below SMA50 (bearish signal)
    if sma_20[index-1] >= sma_50[index-1] and sma_20[index] < sma_50[index] and position > 0:
        # Sell entire position
        return {""action"": ""sell"", ""quantity"": position, ""reason"": f""Death cross: SMA20 ({sma_20[index]:.2f}) < SMA50 ({sma_50[index]:.2f})""}

    # No signal
    return {""action"": ""hold"", ""quantity"": 0, ""reason"": ""No crossover detected""}
";

    protected override async Task OnInitializedAsync()
    {
        if (IsEditMode)
        {
            await LoadStrategy();
        }
        else
        {
            // Set default author from settings or user (for now, just a placeholder)
            _formModel.Author = "Current User";

            // Initialize Python code with default template
            _formModel.PythonCode = DefaultPythonCode;
        }
    }

    private async Task LoadStrategy()
    {
        _isLoading = true;
        _loadingMessage = "Loading strategy...";

        try
        {
            Result<CustomStrategyResult> getResult = await CustomStrategyUseCase.GetStrategyByIdAsync(Id!.Value);

            if (getResult.IsFailure)
            {
                await ShowErrorAsync(string.Join(", ", getResult.Errors.Select(e => e.Message)));
                NavigateToLibrary();
                return;
            }

            CustomStrategyResult result = getResult.Value;

            // Populate form model from result
            _formModel.Name = result.Name;
            _formModel.Description = result.Description;
            _formModel.Author = result.Author;
            _formModel.Category = result.Category;
            _formModel.StrategyType = result.StrategyType;

            // Load type-specific data
            if (result.StrategyType == CustomStrategyType.Python)
            {
                _formModel.PythonCode = result.PythonCode ?? string.Empty;
            }
            else if (result.StrategyType == CustomStrategyType.RuleBased && result.Definition is not null)
            {
                _formModel.SizingMode = result.Definition.SizingMode;

            // Set sizing parameters (convert from 0-1 scale to 1-100 scale for UI)
            if (result.Definition.SizingParameters.TryGetValue("Percentage", out decimal percentage))
            {
                _formModel.FixedPercentage = percentage * 100m;
            }
            if (result.Definition.SizingParameters.TryGetValue("Quantity", out decimal quantity))
            {
                _formModel.FixedQuantity = (int)quantity;
            }
            if (result.Definition.SizingParameters.TryGetValue("RiskPercentage", out decimal risk))
            {
                _formModel.RiskPercentage = risk * 100m;
            }

                // Convert domain rules to form models
                _formModel.EntryRules = result.Definition.EntryRules
                    .Select(RuleFormModel.FromStrategyRule)
                    .ToList();

                _formModel.ExitRules = result.Definition.ExitRules
                    .Select(RuleFormModel.FromStrategyRule)
                    .ToList();
            }

            // Update breadcrumbs for edit mode
            _breadcrumbs = new List<Shared.BreadcrumbNav.Breadcrumb>
            {
                new() { Label = "Dashboard", Href = "/" },
                new() { Label = "Strategy Library", Href = "/strategies/library" },
                new() { Label = result.Name, Href = $"/strategies/builder/{Id}" }
            };
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to load strategy: {ex.Message}");
            NavigateToLibrary();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void HandleInvalidSubmit()
    {
        Log($"[StrategyBuilder] ========== HandleInvalidSubmit called ==========");
        Log($"[StrategyBuilder] Form has data annotation validation errors!");
        Log($"[StrategyBuilder] Form model state:");
        Log($"[StrategyBuilder]   Name: '{_formModel.Name}' (length: {_formModel.Name?.Length ?? 0})");
        Log($"[StrategyBuilder]   Author: '{_formModel.Author}' (length: {_formModel.Author?.Length ?? 0})");
        Log($"[StrategyBuilder]   Category: '{_formModel.Category}' (length: {_formModel.Category?.Length ?? 0})");
        Log($"[StrategyBuilder]   Description: '{_formModel.Description}' (length: {_formModel.Description?.Length ?? 0})");
        Log($"[StrategyBuilder]   Entry rules: {_formModel.EntryRules.Count}");
        Log($"[StrategyBuilder]   Exit rules: {_formModel.ExitRules.Count}");
    }

    private async Task HandleValidSubmit()
    {
        Log($"[StrategyBuilder] ========== HandleValidSubmit called ==========");
        Log($"[StrategyBuilder] IsEditMode: {IsEditMode}");
        Log($"[StrategyBuilder] Strategy Type: {_formModel.StrategyType}");
        Log($"[StrategyBuilder] Form model - Name: {_formModel.Name}, Author: {_formModel.Author}, Category: {_formModel.Category}");

        // Validate based on strategy type
        _validationErrors.Clear();

        if (_formModel.StrategyType == CustomStrategyType.RuleBased)
        {
            Log($"[StrategyBuilder] Entry rules count: {_formModel.EntryRules.Count}");
            Log($"[StrategyBuilder] Exit rules count: {_formModel.ExitRules.Count}");

            if (!_formModel.EntryRules.Any())
            {
                _validationErrors.Add("At least one entry rule is required");
                Log($"[StrategyBuilder] VALIDATION ERROR: No entry rules");
            }

            if (!_formModel.ExitRules.Any())
            {
                _validationErrors.Add("At least one exit rule is required");
                Log($"[StrategyBuilder] VALIDATION ERROR: No exit rules");
            }

            // Validate each entry rule
            for (int i = 0; i < _formModel.EntryRules.Count; i++)
            {
                Log($"[StrategyBuilder] Validating entry rule {i}: {_formModel.EntryRules[i].IndicatorName}");
                ValidateRule(_formModel.EntryRules[i], $"Entry rule {i + 1}");
            }

            // Validate each exit rule
            for (int i = 0; i < _formModel.ExitRules.Count; i++)
            {
                Log($"[StrategyBuilder] Validating exit rule {i}: {_formModel.ExitRules[i].IndicatorName}");
                ValidateRule(_formModel.ExitRules[i], $"Exit rule {i + 1}");
            }
        }
        else if (_formModel.StrategyType == CustomStrategyType.Python)
        {
            Log($"[StrategyBuilder] Python code length: {_formModel.PythonCode?.Length ?? 0}");

            if (string.IsNullOrWhiteSpace(_formModel.PythonCode))
            {
                _validationErrors.Add("Python code is required");
                Log($"[StrategyBuilder] VALIDATION ERROR: No Python code");
            }
        }

        if (_validationErrors.Any())
        {
            Log($"[StrategyBuilder] ========== Validation failed with {_validationErrors.Count} errors ==========");
            foreach (string error in _validationErrors)
            {
                Log($"[StrategyBuilder]   - {error}");
            }
            return;
        }

        Log($"[StrategyBuilder] Validation passed! Proceeding with save...");

        _isSaving = true;

        try
        {
            if (IsEditMode)
            {
                UpdateCustomStrategyCommand command;

                if (_formModel.StrategyType == CustomStrategyType.Python)
                {
                    Log($"[StrategyBuilder] Updating Python strategy {Id!.Value}...");
                    command = new UpdateCustomStrategyCommand(
                        Id.Value,
                        _formModel.Name,
                        _formModel.Description,
                        _formModel.Category,
                        _formModel.PythonCode!
                    );
                }
                else
                {
                    Log($"[StrategyBuilder] Creating strategy definition...");
                    StrategyDefinition definition = _formModel.ToStrategyDefinition();
                    Log($"[StrategyBuilder] Definition created. Entry rules: {definition.EntryRules.Count}, Exit rules: {definition.ExitRules.Count}");
                    Log($"[StrategyBuilder] Updating RuleBased strategy {Id!.Value}...");
                    command = new UpdateCustomStrategyCommand(
                        Id.Value,
                        _formModel.Name,
                        _formModel.Description,
                        _formModel.Category,
                        definition
                    );
                }

                Result<CustomStrategyResult> updateResult = await CustomStrategyUseCase.UpdateStrategyAsync(command);

                if (updateResult.IsFailure)
                {
                    await ShowErrorAsync(string.Join(", ", updateResult.Errors.Select(e => e.Message)));
                    return;
                }
            }
            else
            {
                CreateCustomStrategyCommand command;

                if (_formModel.StrategyType == CustomStrategyType.Python)
                {
                    Log($"[StrategyBuilder] Creating new Python strategy '{_formModel.Name}'...");
                    command = new CreateCustomStrategyCommand(
                        _formModel.Name,
                        _formModel.Description,
                        _formModel.Author,
                        _formModel.Category,
                        _formModel.PythonCode!
                    );
                }
                else
                {
                    Log($"[StrategyBuilder] Creating strategy definition...");
                    StrategyDefinition definition = _formModel.ToStrategyDefinition();
                    Log($"[StrategyBuilder] Definition created. Entry rules: {definition.EntryRules.Count}, Exit rules: {definition.ExitRules.Count}");
                    Log($"[StrategyBuilder] Creating new RuleBased strategy '{_formModel.Name}'...");
                    command = new CreateCustomStrategyCommand(
                        _formModel.Name,
                        _formModel.Description,
                        _formModel.Author,
                        _formModel.Category,
                        definition
                    );
                }

                Result<CustomStrategyResult> createResult = await CustomStrategyUseCase.CreateStrategyAsync(command);

                if (createResult.IsFailure)
                {
                    await ShowErrorAsync(string.Join(", ", createResult.Errors.Select(e => e.Message)));
                    return;
                }

                Log($"[StrategyBuilder] Strategy created successfully!");
            }

            // Navigate first BEFORE showing notification (notification can hang in tests due to JSInterop)
            Log($"[StrategyBuilder] Navigating to library...");
            NavigateToLibrary();
            Log($"[StrategyBuilder] Navigation initiated.");

            // Show success notification (fire-and-forget - don't block on this)
            _ = ShowSuccessAsync(IsEditMode ? "Strategy updated successfully!" : "Strategy created successfully!");
        }
        catch (Exception ex)
        {
            Log($"[StrategyBuilder] ERROR: {ex.GetType().Name}: {ex.Message}");
            Log($"[StrategyBuilder] Stack trace: {ex.StackTrace}");
            await ShowErrorAsync($"Failed to save strategy: {ex.Message}");
        }
        finally
        {
            _isSaving = false;
        }
    }

    private void ValidateRule(RuleFormModel rule, string ruleName)
    {
        if (string.IsNullOrWhiteSpace(rule.IndicatorName))
        {
            _validationErrors.Add($"{ruleName}: Indicator is required");
        }

        if (rule.ValueType == RuleValueType.Constant && rule.ConstantValue is null)
        {
            _validationErrors.Add($"{ruleName}: Constant value is required");
        }

        if (rule.ValueType == RuleValueType.Indicator && string.IsNullOrWhiteSpace(rule.SecondIndicatorName))
        {
            _validationErrors.Add($"{ruleName}: Second indicator is required for indicator comparison");
        }
    }

    private async Task HandleDelete()
    {
        if (!IsEditMode)
        {
            return;
        }

        bool confirmed = await JSRuntime.InvokeAsync<bool>("confirm", "Are you sure you want to delete this strategy? This action cannot be undone.");

        if (!confirmed)
        {
            return;
        }

        try
        {
            Result<bool> deleteResult = await CustomStrategyUseCase.DeleteStrategyAsync(Id!.Value);

            if (deleteResult.IsFailure)
            {
                await ShowErrorAsync(string.Join(", ", deleteResult.Errors.Select(e => e.Message)));
                return;
            }

            await ShowSuccessAsync("Strategy deleted successfully!");
            NavigateToLibrary();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to delete strategy: {ex.Message}");
        }
    }

    private void NavigateToLibrary()
    {
        Navigation.NavigateTo("/strategies/library");
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

    private async Task HandleValidateSyntax()
    {
        _isValidating = true;
        _pythonValidationErrors.Clear();

        try
        {
            Log($"[StrategyBuilder] Validating Python syntax...");

            var command = new ValidatePythonCodeCommand(_formModel.PythonCode ?? string.Empty);
            Result<PythonValidationResult> validationResult = await CustomStrategyUseCase.ValidatePythonCodeAsync(command);

            if (validationResult.IsFailure)
            {
                _pythonValidationErrors.Add($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
                return;
            }

            if (!validationResult.Value.IsValid)
            {
                _pythonValidationErrors.AddRange(validationResult.Value.Errors);
                Log($"[StrategyBuilder] Syntax validation failed with {_pythonValidationErrors.Count} errors");
            }
            else
            {
                await ShowSuccessAsync("Python code is valid!");
                Log($"[StrategyBuilder] Syntax validation passed");
            }
        }
        catch (Exception ex)
        {
            Log($"[StrategyBuilder] Error during validation: {ex.Message}");
            await ShowErrorAsync($"Validation error: {ex.Message}");
        }
        finally
        {
            _isValidating = false;
        }
    }

    private async Task HandleDryRun()
    {
        _isDryRunning = true;
        _dryRunResult = null;
        _pythonValidationErrors.Clear();

        try
        {
            Log($"[StrategyBuilder] Starting dry run...");

            var command = new DryRunPythonStrategyCommand(
                _formModel.PythonCode ?? string.Empty,
                "AAPL", // Default ticker for dry run
                InitialCash: 10000m
            );
            Result<DryRunResult> dryRunResult = await CustomStrategyUseCase.DryRunPythonStrategyAsync(command);

            if (dryRunResult.IsFailure)
            {
                await ShowErrorAsync($"Dry run failed: {string.Join(", ", dryRunResult.Errors.Select(e => e.Message))}");
                return;
            }

            _dryRunResult = dryRunResult.Value;

            if (_dryRunResult.IsValid)
            {
                await ShowSuccessAsync($"Dry run completed! {_dryRunResult.TotalTrades} trades, {_dryRunResult.TotalReturn:P2} return");
                Log($"[StrategyBuilder] Dry run successful: {_dryRunResult.TotalTrades} trades");
            }
            else
            {
                Log($"[StrategyBuilder] Dry run failed with {_dryRunResult.ValidationErrors.Count} errors");
            }
        }
        catch (Exception ex)
        {
            Log($"[StrategyBuilder] Error during dry run: {ex.Message}");
            await ShowErrorAsync($"Dry run error: {ex.Message}");
        }
        finally
        {
            _isDryRunning = false;
        }
    }

    private async Task HandleMonacoError(string errorMessage)
    {
        _validationErrors.Add($"Monaco Editor Error: {errorMessage}");
        await NotificationService.ShowErrorAsync(
            "Editor Failed",
            $"Monaco editor failed to initialize: {errorMessage}");
        StateHasChanged();
    }
}
