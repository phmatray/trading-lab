using System.Text.Json;
using TradingStrat.Web.Services;

namespace TradingStrat.ComponentTests.TestDoubles;

/// <summary>
/// Fake implementation of LocalStorageService for testing.
/// Stores data in-memory instead of browser localStorage.
/// </summary>
public class FakeLocalStorageService : LocalStorageService
{
    private readonly Dictionary<string, string> _storage = new();

    public FakeLocalStorageService() : base(null!)
    {
        // No JSRuntime needed for fake implementation
    }

    public new async Task<T?> GetItemAsync<T>(
        string key,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async

        if (!_storage.TryGetValue(key, out string? json))
        {
            return default;
        }

        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        return JsonSerializer.Deserialize<T>(json, options);
    }

    public new async Task SetItemAsync<T>(
        string key,
        T value,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async

        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        string json = JsonSerializer.Serialize(value, options);
        _storage[key] = json;
    }

    public new async Task RemoveItemAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async
        _storage.Remove(key);
    }

    /// <summary>
    /// Clears all items from the fake storage.
    /// Useful for resetting state between tests.
    /// </summary>
    public void ClearAll()
    {
        _storage.Clear();
        ClearCache();
    }

    /// <summary>
    /// Gets the number of items in storage.
    /// </summary>
    public int Count => _storage.Count;

    /// <summary>
    /// Checks if a key exists in storage.
    /// </summary>
    public bool ContainsKey(string key) => _storage.ContainsKey(key);
}
