using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
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

    private readonly StrategyFormModel formModel = new();
    private bool isLoading = false;
    private bool isSaving = false;
    private string loadingMessage = "Loading...";
    private readonly List<string> validationErrors = [];

    protected override async Task OnInitializedAsync()
    {
        if (IsEditMode)
        {
            await LoadStrategy();
        }
        else
        {
            // Set default author from settings or user (for now, just a placeholder)
            formModel.Author = "Current User";
        }
    }

    private async Task LoadStrategy()
    {
        isLoading = true;
        loadingMessage = "Loading strategy...";

        try
        {
            CustomStrategyResult? result = await CustomStrategyUseCase.GetStrategyByIdAsync(Id!.Value);

            if (result == null)
            {
                await ShowErrorAsync("Strategy not found");
                NavigateToLibrary();
                return;
            }

            // Populate form model from result
            formModel.Name = result.Name;
            formModel.Description = result.Description;
            formModel.Author = result.Author;
            formModel.Category = result.Category;
            formModel.SizingMode = result.Definition.SizingMode;

            // Set sizing parameters (convert from 0-1 scale to 1-100 scale for UI)
            if (result.Definition.SizingParameters.TryGetValue("Percentage", out decimal percentage))
            {
                formModel.FixedPercentage = percentage * 100m;
            }
            if (result.Definition.SizingParameters.TryGetValue("Quantity", out decimal quantity))
            {
                formModel.FixedQuantity = (int)quantity;
            }
            if (result.Definition.SizingParameters.TryGetValue("RiskPercentage", out decimal risk))
            {
                formModel.RiskPercentage = risk * 100m;
            }

            // Convert domain rules to form models
            formModel.EntryRules = result.Definition.EntryRules
                .Select(RuleFormModel.FromStrategyRule)
                .ToList();

            formModel.ExitRules = result.Definition.ExitRules
                .Select(RuleFormModel.FromStrategyRule)
                .ToList();
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to load strategy: {ex.Message}");
            NavigateToLibrary();
        }
        finally
        {
            isLoading = false;
        }
    }

    private void HandleInvalidSubmit()
    {
        Log($"[StrategyBuilder] ========== HandleInvalidSubmit called ==========");
        Log($"[StrategyBuilder] Form has data annotation validation errors!");
        Log($"[StrategyBuilder] Form model state:");
        Log($"[StrategyBuilder]   Name: '{formModel.Name}' (length: {formModel.Name?.Length ?? 0})");
        Log($"[StrategyBuilder]   Author: '{formModel.Author}' (length: {formModel.Author?.Length ?? 0})");
        Log($"[StrategyBuilder]   Category: '{formModel.Category}' (length: {formModel.Category?.Length ?? 0})");
        Log($"[StrategyBuilder]   Description: '{formModel.Description}' (length: {formModel.Description?.Length ?? 0})");
        Log($"[StrategyBuilder]   Entry rules: {formModel.EntryRules.Count}");
        Log($"[StrategyBuilder]   Exit rules: {formModel.ExitRules.Count}");
    }

    private async Task HandleValidSubmit()
    {
        Log($"[StrategyBuilder] ========== HandleValidSubmit called ==========");
        Log($"[StrategyBuilder] IsEditMode: {IsEditMode}");
        Log($"[StrategyBuilder] Form model - Name: {formModel.Name}, Author: {formModel.Author}, Category: {formModel.Category}");
        Log($"[StrategyBuilder] Entry rules count: {formModel.EntryRules.Count}");
        Log($"[StrategyBuilder] Exit rules count: {formModel.ExitRules.Count}");

        // Validate rules
        validationErrors.Clear();

        if (!formModel.EntryRules.Any())
        {
            validationErrors.Add("At least one entry rule is required");
            Log($"[StrategyBuilder] VALIDATION ERROR: No entry rules");
        }

        if (!formModel.ExitRules.Any())
        {
            validationErrors.Add("At least one exit rule is required");
            Log($"[StrategyBuilder] VALIDATION ERROR: No exit rules");
        }

        // Validate each entry rule
        for (int i = 0; i < formModel.EntryRules.Count; i++)
        {
            Log($"[StrategyBuilder] Validating entry rule {i}: {formModel.EntryRules[i].IndicatorName}");
            ValidateRule(formModel.EntryRules[i], $"Entry rule {i + 1}");
        }

        // Validate each exit rule
        for (int i = 0; i < formModel.ExitRules.Count; i++)
        {
            Log($"[StrategyBuilder] Validating exit rule {i}: {formModel.ExitRules[i].IndicatorName}");
            ValidateRule(formModel.ExitRules[i], $"Exit rule {i + 1}");
        }

        if (validationErrors.Any())
        {
            Log($"[StrategyBuilder] ========== Validation failed with {validationErrors.Count} errors ==========");
            foreach (string error in validationErrors)
            {
                Log($"[StrategyBuilder]   - {error}");
            }
            return;
        }

        Log($"[StrategyBuilder] Validation passed! Proceeding with save...");

        isSaving = true;

        try
        {
            Log($"[StrategyBuilder] Creating strategy definition...");
            StrategyDefinition definition = formModel.ToStrategyDefinition();
            Log($"[StrategyBuilder] Definition created. Entry rules: {definition.EntryRules.Count}, Exit rules: {definition.ExitRules.Count}");

            if (IsEditMode)
            {
                var command = new UpdateCustomStrategyCommand(
                    Id!.Value,
                    formModel.Name,
                    formModel.Description,
                    formModel.Category,
                    definition
                );

                Log($"[StrategyBuilder] Updating strategy {Id.Value}...");
                await CustomStrategyUseCase.UpdateStrategyAsync(command);
            }
            else
            {
                var command = new CreateCustomStrategyCommand(
                    formModel.Name,
                    formModel.Description,
                    formModel.Author,
                    formModel.Category,
                    definition
                );

                Log($"[StrategyBuilder] Creating new strategy '{formModel.Name}'...");
                await CustomStrategyUseCase.CreateStrategyAsync(command);
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
            isSaving = false;
        }
    }

    private void ValidateRule(RuleFormModel rule, string ruleName)
    {
        if (string.IsNullOrWhiteSpace(rule.IndicatorName))
        {
            validationErrors.Add($"{ruleName}: Indicator is required");
        }

        if (rule.ValueType == RuleValueType.Constant && rule.ConstantValue == null)
        {
            validationErrors.Add($"{ruleName}: Constant value is required");
        }

        if (rule.ValueType == RuleValueType.Indicator && string.IsNullOrWhiteSpace(rule.SecondIndicatorName))
        {
            validationErrors.Add($"{ruleName}: Second indicator is required for indicator comparison");
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
            await CustomStrategyUseCase.DeleteStrategyAsync(Id!.Value);
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
