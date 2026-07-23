using Microsoft.AspNetCore.Components;
using TradingStrat.Domain.Common;
using TradingStrat.Web.Services;
using TradingStrat.Web.Services.State;

namespace TradingStrat.Web.Components.Pages;

/// <summary>
/// Abstract base class for pages that perform data operations (fetch, backtest, analysis, etc.).
/// Centralizes common patterns: progress reporting, form state persistence, error handling, and lifecycle management.
/// Supports Result&lt;T&gt; pattern for consistent error handling.
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
    /// <returns>A Result containing the operation result, or errors if the operation failed.</returns>
    protected abstract Task<Result<TResult>> ExecuteOperationAsync(TFormModel model, IProgress<string> progress);

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
    /// Automatically handles Result&lt;T&gt; pattern for consistent error handling.
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
            // Execute the operation (returns Result<TResult>)
            Result<TResult> result = await ExecuteOperationAsync(FormModel, progress);

            if (result.IsSuccess)
            {
                // Extract the value from the successful result
                Result = result.Value;

                // Display success message
                SuccessMessage = GetSuccessMessage(Result);

                // Persist form state for next visit
                await FormState.SaveFormStateAsync(FormKey, FormModel);
            }
            else
            {
                // Display structured error messages from Result<T>
                ErrorMessage = FormatErrors(result.Errors);
            }
        }
        catch (Exception ex)
        {
            // This catch block now only handles unexpected system errors
            // (business logic errors should be returned as Result<T>.Failure)
            ErrorMessage = $"Unexpected error: {ex.Message}";
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
    /// Formats a list of errors into a user-friendly error message.
    /// Override to provide custom error formatting.
    /// </summary>
    /// <param name="errors">The list of errors from a failed Result.</param>
    /// <returns>A formatted error message string.</returns>
    protected virtual string FormatErrors(IReadOnlyList<Error> errors)
    {
        if (errors.Count == 0)
        {
            return "An unknown error occurred.";
        }

        if (errors.Count == 1)
        {
            return $"Error: {errors[0].Message}";
        }

        // Multiple errors - format as bullet list
        return "Errors:\n" + string.Join("\n", errors.Select((e, i) => $"• {e.Message}"));
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
