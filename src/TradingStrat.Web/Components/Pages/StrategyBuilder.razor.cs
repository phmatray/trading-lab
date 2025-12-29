using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.Common;
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

    private readonly StrategyFormModel _formModel = new();
    private bool _isLoading;
    private bool _isSaving;
    private string _loadingMessage = "Loading...";
    private readonly List<string> _validationErrors = [];

    private List<Shared.BreadcrumbNav.Breadcrumb> _breadcrumbs = new()
    {
        new() { Label = "Dashboard", Href = "/" },
        new() { Label = "Strategy Library", Href = "/strategies/library" },
        new() { Label = "Strategy Builder", Href = "/strategies/builder" }
    };

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
        Log($"[StrategyBuilder] Form model - Name: {_formModel.Name}, Author: {_formModel.Author}, Category: {_formModel.Category}");
        Log($"[StrategyBuilder] Entry rules count: {_formModel.EntryRules.Count}");
        Log($"[StrategyBuilder] Exit rules count: {_formModel.ExitRules.Count}");

        // Validate rules
        _validationErrors.Clear();

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
            Log($"[StrategyBuilder] Creating strategy definition...");
            StrategyDefinition definition = _formModel.ToStrategyDefinition();
            Log($"[StrategyBuilder] Definition created. Entry rules: {definition.EntryRules.Count}, Exit rules: {definition.ExitRules.Count}");

            if (IsEditMode)
            {
                var command = new UpdateCustomStrategyCommand(
                    Id!.Value,
                    _formModel.Name,
                    _formModel.Description,
                    _formModel.Category,
                    definition
                );

                Log($"[StrategyBuilder] Updating strategy {Id.Value}...");
                Result<CustomStrategyResult> updateResult = await CustomStrategyUseCase.UpdateStrategyAsync(command);

                if (updateResult.IsFailure)
                {
                    await ShowErrorAsync(string.Join(", ", updateResult.Errors.Select(e => e.Message)));
                    return;
                }
            }
            else
            {
                var command = new CreateCustomStrategyCommand(
                    _formModel.Name,
                    _formModel.Description,
                    _formModel.Author,
                    _formModel.Category,
                    definition
                );

                Log($"[StrategyBuilder] Creating new strategy '{_formModel.Name}'...");
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
}
