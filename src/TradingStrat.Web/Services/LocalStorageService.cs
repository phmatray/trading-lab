using System.Text.Json;
using Microsoft.JSInterop;

namespace TradingStrat.Web.Services;

public class LocalStorageService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Dictionary<string, object?> _cache = new();

    public LocalStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<T?> GetItemAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check cache first
            if (_cache.TryGetValue(key, out object? cached))
            {
                return (T?)cached;
            }

            string? json = await _jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem",
                cancellationToken,
                key);

            if (string.IsNullOrEmpty(json))
            {
                return default;
            }

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };

            T? item = JsonSerializer.Deserialize<T>(json, options);
            _cache[key] = item;
            return item;
        }
        catch (JSException ex)
        {
            // Log error but don't throw - localStorage may be disabled
            Console.WriteLine($"localStorage.getItem failed for key '{key}': {ex.Message}");
            return default;
        }
    }

    public async Task SetItemAsync<T>(
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            };

            string json = JsonSerializer.Serialize(value, options);

            await _jsRuntime.InvokeVoidAsync(
                "localStorage.setItem",
                cancellationToken,
                key,
                json);

            _cache[key] = value;
        }
        catch (JSException ex)
        {
            Console.WriteLine($"localStorage.setItem failed for key '{key}': {ex.Message}");
            // Don't throw - gracefully degrade if localStorage unavailable
        }
    }

    public async Task RemoveItemAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "localStorage.removeItem",
                cancellationToken,
                key);

            _cache.Remove(key);
        }
        catch (JSException ex)
        {
            Console.WriteLine($"localStorage.removeItem failed for key '{key}': {ex.Message}");
        }
    }

    public void ClearCache()
    {
        _cache.Clear();
    }
}
