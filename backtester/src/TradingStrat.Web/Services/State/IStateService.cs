namespace TradingStrat.Web.Services.State;

/// <summary>
/// Generic interface for state services that manage application state with localStorage persistence.
/// </summary>
/// <typeparam name="T">The type of state to manage.</typeparam>
public interface IStateService<T> where T : class, new()
{
    /// <summary>
    /// Gets the current state from memory or localStorage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current state, or a new instance if none exists.</returns>
    Task<T> GetStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the state to memory and localStorage.
    /// </summary>
    /// <param name="state">The state to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveStateAsync(T state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the state from memory and localStorage.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Event raised when the state changes.
    /// </summary>
    event Action? OnStateChanged;
}
