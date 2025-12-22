using System.Text.Json;
using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services.State;

/// <summary>
/// Service for managing form state persistence with debouncing and automatic cleanup.
/// Stores multiple form states in a single localStorage container.
/// </summary>
public class FormStateService : StateServiceBase<FormStateContainer>
{
    private const string STORAGE_KEY = "tradingstrat_form_states";
    private const int CLEANUP_DAYS = 7;
    private readonly Dictionary<string, Timer> _debounceTimers = new();

    public FormStateService(LocalStorageService localStorage)
        : base(localStorage, STORAGE_KEY)
    {
    }

    /// <summary>
    /// Gets a specific form state by key.
    /// </summary>
    public async Task<T?> GetFormStateAsync<T>(
        string formKey,
        CancellationToken cancellationToken = default)
    {
        FormStateContainer container = await GetStateAsync(cancellationToken);

        if (container.SavedForms.TryGetValue(formKey, out string? json))
        {
            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                // Invalid JSON, remove it
                container.SavedForms.Remove(formKey);
                await SaveStateAsync(container, cancellationToken);
            }
        }

        return default;
    }

    /// <summary>
    /// Saves a form state with debouncing to avoid excessive localStorage writes.
    /// </summary>
    public Task SaveFormStateAsync<T>(
        string formKey,
        T formModel,
        int debounceMs = 500,
        CancellationToken cancellationToken = default)
    {
        // Dispose existing timer for this form key
        if (_debounceTimers.TryGetValue(formKey, out Timer? existingTimer))
        {
            existingTimer.Dispose();
        }

        // Create new debounced timer
        Timer timer = new(async _ =>
        {
            FormStateContainer container = await GetStateAsync(cancellationToken);
            container.SavedForms[formKey] = JsonSerializer.Serialize(formModel);
            await SaveStateAsync(container, cancellationToken);
            _debounceTimers.Remove(formKey);
        }, null, debounceMs, Timeout.Infinite);

        _debounceTimers[formKey] = timer;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears a specific form state.
    /// </summary>
    public async Task ClearFormStateAsync(
        string formKey,
        CancellationToken cancellationToken = default)
    {
        FormStateContainer container = await GetStateAsync(cancellationToken);
        container.SavedForms.Remove(formKey);
        await SaveStateAsync(container, cancellationToken);
    }

    /// <summary>
    /// Creates a new container and performs cleanup if the last cleanup was more than CLEANUP_DAYS ago.
    /// </summary>
    protected override async Task<FormStateContainer> CreateDefaultStateAsync()
    {
        FormStateContainer container = await base.CreateDefaultStateAsync() ?? new FormStateContainer();

        // Cleanup old forms if needed
        if ((DateTime.UtcNow - container.LastCleanup).TotalDays >= CLEANUP_DAYS)
        {
            container = new FormStateContainer();
        }

        container.LastCleanup = DateTime.UtcNow;
        return container;
    }
}
