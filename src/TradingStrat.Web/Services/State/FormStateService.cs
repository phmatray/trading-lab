using System.Text.Json;
using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Services.State;

public class FormStateService
{
    private const string STORAGE_KEY = "tradingstrat_form_states";
    private const int CLEANUP_DAYS = 7;
    private readonly LocalStorageService _localStorage;
    private readonly Dictionary<string, Timer> _debounceTimers = new();

    public FormStateService(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<T?> GetFormStateAsync<T>(
        string formKey,
        CancellationToken cancellationToken = default)
    {
        FormStateContainer container = await GetContainerAsync(cancellationToken);

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
                await SaveContainerAsync(container, cancellationToken);
            }
        }

        return default;
    }

    public async Task SaveFormStateAsync<T>(
        string formKey,
        T formModel,
        int debounceMs = 500,
        CancellationToken cancellationToken = default)
    {
        // Debounce to avoid excessive localStorage writes
        if (_debounceTimers.TryGetValue(formKey, out Timer? existingTimer))
        {
            existingTimer.Dispose();
        }

        Timer timer = new Timer(async _ =>
        {
            FormStateContainer container = await GetContainerAsync(cancellationToken);
            string json = JsonSerializer.Serialize(formModel);
            container.SavedForms[formKey] = json;
            await SaveContainerAsync(container, cancellationToken);

            _debounceTimers.Remove(formKey);
        }, null, debounceMs, Timeout.Infinite);

        _debounceTimers[formKey] = timer;
    }

    public async Task ClearFormStateAsync(
        string formKey,
        CancellationToken cancellationToken = default)
    {
        var container = await GetContainerAsync(cancellationToken);
        container.SavedForms.Remove(formKey);
        await SaveContainerAsync(container, cancellationToken);
    }

    private async Task<FormStateContainer> GetContainerAsync(
        CancellationToken cancellationToken)
    {
        var container = await _localStorage.GetItemAsync<FormStateContainer>(
            STORAGE_KEY,
            cancellationToken);

        if (container == null)
        {
            return new FormStateContainer();
        }

        // Cleanup old forms if needed
        if ((DateTime.UtcNow - container.LastCleanup).TotalDays >= CLEANUP_DAYS)
        {
            // For simplicity, clear all - could implement per-form timestamps
            container = new FormStateContainer();
        }

        return container;
    }

    private async Task SaveContainerAsync(
        FormStateContainer container,
        CancellationToken cancellationToken)
    {
        container.LastCleanup = DateTime.UtcNow;
        await _localStorage.SetItemAsync(STORAGE_KEY, container, cancellationToken);
    }
}
