using Microsoft.JSInterop;

namespace TradingStrat.Web.Services.State;

/// <summary>
/// Abstract base class for state services that manage application state with localStorage persistence.
/// Centralizes localStorage error handling and caching to eliminate code duplication.
/// </summary>
/// <typeparam name="T">The type of state to manage.</typeparam>
public abstract class StateServiceBase<T> : IStateService<T> where T : class, new()
{
    private readonly LocalStorageService _localStorage;
    private readonly string _storageKey;
    private T? _cachedState;

    /// <summary>
    /// Event raised when the state changes.
    /// </summary>
    public event Action? OnStateChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="StateServiceBase{T}"/> class.
    /// </summary>
    /// <param name="localStorage">The local storage service.</param>
    /// <param name="storageKey">The key to use for localStorage persistence.</param>
    protected StateServiceBase(LocalStorageService localStorage, string storageKey)
    {
        _localStorage = localStorage;
        _storageKey = storageKey;
    }

    /// <summary>
    /// Gets the current state from memory or localStorage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current state, or a new instance if none exists.</returns>
    public virtual async Task<T> GetStateAsync(CancellationToken cancellationToken = default)
    {
        // Return cached state if available
        if (_cachedState != null)
        {
            return _cachedState;
        }

        // Try to load from localStorage
        T? state = await SafeLocalStorageCall(
            async () => await _localStorage.GetItemAsync<T?>(_storageKey, cancellationToken),
            fallbackValue: null);

        // Use loaded state or create new instance
        _cachedState = state ?? await CreateDefaultStateAsync();
        return _cachedState;
    }

    /// <summary>
    /// Saves the state to memory and localStorage.
    /// </summary>
    /// <param name="state">The state to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public virtual async Task SaveStateAsync(T state, CancellationToken cancellationToken = default)
    {
        _cachedState = state;

        await SafeLocalStorageCall(
            async () =>
            {
                await _localStorage.SetItemAsync(_storageKey, state, cancellationToken);
                return true;
            },
            fallbackValue: false);

        NotifyStateChanged();
    }

    /// <summary>
    /// Clears the state from memory and localStorage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public virtual async Task ClearStateAsync(CancellationToken cancellationToken = default)
    {
        _cachedState = null;

        await SafeLocalStorageCall(
            async () =>
            {
                await _localStorage.RemoveItemAsync(_storageKey, cancellationToken);
                return true;
            },
            fallbackValue: false);

        NotifyStateChanged();
    }

    /// <summary>
    /// Safely executes a localStorage operation with centralized error handling.
    /// Catches InvalidOperationException (JS interop not available) and JSDisconnectedException (circuit disconnected).
    /// </summary>
    /// <typeparam name="TResult">The result type of the operation.</typeparam>
    /// <param name="operation">The localStorage operation to execute.</param>
    /// <param name="fallbackValue">The value to return if the operation fails.</param>
    /// <returns>The result of the operation, or the fallback value if it fails.</returns>
    protected async Task<TResult?> SafeLocalStorageCall<TResult>(
        Func<Task<TResult?>> operation,
        TResult? fallbackValue = default)
    {
        try
        {
            return await operation();
        }
        catch (InvalidOperationException)
        {
            // JS interop not available (static rendering), return fallback
            return fallbackValue;
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected, return fallback
            return fallbackValue;
        }
    }

    /// <summary>
    /// Creates a default state instance. Override to provide custom initialization logic.
    /// </summary>
    /// <returns>A new instance of the state.</returns>
    protected virtual Task<T> CreateDefaultStateAsync()
    {
        return Task.FromResult(new T());
    }

    /// <summary>
    /// Notifies subscribers that the state has changed.
    /// </summary>
    protected void NotifyStateChanged()
    {
        OnStateChanged?.Invoke();
    }
}
