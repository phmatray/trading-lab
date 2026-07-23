using TradingStrat.Web.Services;

namespace TradingStrat.ComponentTests.TestDoubles;

/// <summary>
/// Fake implementation of ProgressService for testing.
/// Provides in-memory progress tracking without side effects.
/// </summary>
public class FakeProgressService : ProgressService
{
    public new event Action? OnProgressChanged;

    public new string CurrentMessage { get; private set; } = string.Empty;
    public new int? CurrentProgress { get; private set; }
    public new bool IsProcessing { get; private set; }
    public new ProgressState State { get; private set; } = ProgressState.Idle;

    public new void UpdateProgress(string message, int? progress = null)
    {
        CurrentMessage = message;
        CurrentProgress = progress;
        State = ProgressState.Processing;
        IsProcessing = true;
        OnProgressChanged?.Invoke();
    }

    public new void SetSuccess(string message)
    {
        CurrentMessage = message;
        CurrentProgress = 100;
        State = ProgressState.Success;
        IsProcessing = false;
        OnProgressChanged?.Invoke();
    }

    public new void SetError(string message)
    {
        CurrentMessage = message;
        CurrentProgress = null;
        State = ProgressState.Error;
        IsProcessing = false;
        OnProgressChanged?.Invoke();
    }

    public new void Reset()
    {
        CurrentMessage = string.Empty;
        CurrentProgress = null;
        State = ProgressState.Idle;
        IsProcessing = false;
        OnProgressChanged?.Invoke();
    }

    /// <summary>
    /// Resets the service and clears all event subscriptions.
    /// Useful for test cleanup.
    /// </summary>
    public void ResetAll()
    {
        Reset();
        OnProgressChanged = null;
    }

    public new void Dispose()
    {
        OnProgressChanged = null;
        base.Dispose();
    }
}
