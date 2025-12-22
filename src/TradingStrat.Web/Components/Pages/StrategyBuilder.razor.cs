using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TradingStrat.Application.Commands;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;

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

            // Set sizing parameters
            if (result.Definition.SizingParameters.TryGetValue("Percentage", out decimal percentage))
            {
                formModel.FixedPercentage = percentage;
            }
            if (result.Definition.SizingParameters.TryGetValue("Quantity", out decimal quantity))
            {
                formModel.FixedQuantity = (int)quantity;
            }
            if (result.Definition.SizingParameters.TryGetValue("RiskPercentage", out decimal risk))
            {
                formModel.RiskPercentage = risk;
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

    private async Task HandleValidSubmit()
    {
        // Validate rules
        validationErrors.Clear();

        if (!formModel.EntryRules.Any())
        {
            validationErrors.Add("At least one entry rule is required");
        }

        if (!formModel.ExitRules.Any())
        {
            validationErrors.Add("At least one exit rule is required");
        }

        // Validate each entry rule
        for (int i = 0; i < formModel.EntryRules.Count; i++)
        {
            ValidateRule(formModel.EntryRules[i], $"Entry rule {i + 1}");
        }

        // Validate each exit rule
        for (int i = 0; i < formModel.ExitRules.Count; i++)
        {
            ValidateRule(formModel.ExitRules[i], $"Exit rule {i + 1}");
        }

        if (validationErrors.Any())
        {
            return;
        }

        isSaving = true;

        try
        {
            StrategyDefinition definition = formModel.ToStrategyDefinition();

            if (IsEditMode)
            {
                var command = new UpdateCustomStrategyCommand(
                    Id!.Value,
                    formModel.Name,
                    formModel.Description,
                    formModel.Category,
                    definition
                );

                await CustomStrategyUseCase.UpdateStrategyAsync(command);
                await ShowSuccessAsync("Strategy updated successfully!");
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

                await CustomStrategyUseCase.CreateStrategyAsync(command);
                await ShowSuccessAsync("Strategy created successfully!");
            }

            NavigateToLibrary();
        }
        catch (Exception ex)
        {
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
