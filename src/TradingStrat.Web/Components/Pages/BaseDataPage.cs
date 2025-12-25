using Microsoft.AspNetCore.Components;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

/// <summary>
/// Abstract base class for pages that perform data operations (fetch, backtest, analysis, etc.).
/// Centralizes common patterns: progress reporting, form state persistence, error handling, and lifecycle management.
/// </summary>
/// <typeparam name="TFormModel">The type of form model used by the page.</typeparam>
/// <typeparam name="TResult">The type of result returned by the operation.</typeparam>
public abstract class BaseDataPage<TFormModel, TResult> : ComponentBase, IDisposable
    where TFormModel : class, new()
{
    /// <summary>
    /// Service for reporting operation progress to the UI.
    /// </summary>
    [Inject]
    protected ProgressService ProgressService { get; set; } = null!;

    /// <summary>
    /// Service for persisting form state to localStorage.
    /// </summary>
    [Inject]
    protected FormStateService FormState { get; set; } = null!;

    /// <summary>
    /// The form model containing user input.
    /// </summary>
    protected TFormModel FormModel { get; set; } = new();

    /// <summary>
    /// The result of the most recent operation.
    /// </summary>
    protected TResult? Result { get; set; }

    /// <summary>
    /// Error message to display to the user, if any.
    /// </summary>
    protected string? ErrorMessage { get; set; }

    /// <summary>
    /// Success message to display to the user, if any.
    /// </summary>
    protected string? SuccessMessage { get; set; }

    /// <summary>
    /// Gets the localStorage key for persisting this page's form state.
    /// Must be unique across the application.
    /// </summary>
    protected abstract string FormKey { get; }

    /// <summary>
    /// Executes the page's primary operation with the provided form model.
    /// Progress updates should be reported via the progress parameter.
    /// </summary>
    /// <param name="model">The form model with user input.</param>
    /// <param name="progress">Progress reporter for status updates.</param>
    /// <returns>The result of the operation.</returns>
    protected abstract Task<TResult> ExecuteOperationAsync(TFormModel model, IProgress<string> progress);

    /// <summary>
    /// Initializes the component. Subscribes to progress updates and restores form state.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        // Subscribe to progress updates (triggers UI refresh)
        ProgressService.OnProgressChanged += StateHasChanged;

        // Restore form state from localStorage (if available)
        TFormModel? savedState = await FormState.GetFormStateAsync<TFormModel>(FormKey);

        // Use saved state, or initialize defaults, or create new instance
        FormModel = savedState ?? await InitializeDefaultsAsync() ?? new TFormModel();
    }

    /// <summary>
    /// Handles form submission. Orchestrates progress reporting, operation execution,
    /// success/error handling, and cleanup.
    /// </summary>
    protected async Task HandleSubmitAsync()
    {
        // Clear previous messages
        ErrorMessage = null;
        SuccessMessage = null;

        // Reset progress indicator (defensive cleanup)
        await InvokeAsync(() => ProgressService.Reset());

        // Create progress reporter that updates ProgressService
        IProgress<string> progress = new Progress<string>(msg =>
            InvokeAsync(() => ProgressService.UpdateProgress(msg)));

        try
        {
            // Execute the operation
            Result = await ExecuteOperationAsync(FormModel, progress);

            // Display success message
            SuccessMessage = GetSuccessMessage(Result);

            // Persist form state for next visit
            await FormState.SaveFormStateAsync(FormKey, FormModel);
        }
        catch (Exception ex)
        {
            // Display error message
            ErrorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            // Always reset progress indicator
            await InvokeAsync(() => ProgressService.Reset());
        }
    }

    /// <summary>
    /// Initializes default form values. Override to provide custom initialization logic.
    /// Called when no saved state is available in localStorage.
    /// </summary>
    /// <returns>The default form model, or null to use a new instance.</returns>
    protected virtual Task<TFormModel?> InitializeDefaultsAsync()
    {
        return Task.FromResult<TFormModel?>(null);
    }

    /// <summary>
    /// Gets the success message to display after operation completion.
    /// Override to provide custom success messages based on the result.
    /// </summary>
    /// <param name="result">The operation result.</param>
    /// <returns>The success message to display.</returns>
    protected virtual string GetSuccessMessage(TResult? result)
    {
        return "Operation completed successfully.";
    }

    /// <summary>
    /// Helper method for handling form property changes with automatic state persistence.
    /// Eliminates repetitive OnXxxChanged handler patterns across pages.
    /// </summary>
    /// <param name="updateAction">Action to update a property on the FormModel.</param>
    /// <returns>Task that completes when the state is persisted.</returns>
    /// <example>
    /// // Instead of:
    /// // private async Task OnStrategyTypeChanged(string value)
    /// // {
    /// //     FormModel.StrategyType = value;
    /// //     await FormState.SaveFormStateAsync(FormKey, FormModel);
    /// //     StateHasChanged();
    /// // }
    /// //
    /// // Use:
    /// // private async Task OnStrategyTypeChanged(string value)
    /// //     => await OnPropertyChangedAsync(m => m.StrategyType = value);
    /// </example>
    protected async Task OnPropertyChangedAsync(Action<TFormModel> updateAction)
    {
        updateAction(FormModel);
        await FormState.SaveFormStateAsync(FormKey, FormModel);
        StateHasChanged();
    }

    /// <summary>
    /// Disposes the component. Unsubscribes from progress updates.
    /// </summary>
    public virtual void Dispose()
    {
        ProgressService.OnProgressChanged -= StateHasChanged;
        GC.SuppressFinalize(this);
    }
}
