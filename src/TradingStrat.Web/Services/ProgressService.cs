namespace TradingStrat.Web.Services;

public enum ProgressState
{
    Idle,
    Processing,
    Success,
    Error
}

public class ProgressService : IDisposable
{
    private string _currentMessage = string.Empty;
    private int? _currentProgress;
    private ProgressState _state = ProgressState.Idle;

    public event Action? OnProgressChanged;

    public string CurrentMessage => _currentMessage;
    public int? CurrentProgress => _currentProgress;
    public bool IsProcessing => _state == ProgressState.Processing;
    public ProgressState State => _state;

    public void UpdateProgress(string message, int? progress = null)
    {
        _currentMessage = message;
        _currentProgress = progress;
        _state = ProgressState.Processing;
        NotifyStateChanged();
    }

    public void SetSuccess(string message)
    {
        _currentMessage = message;
        _currentProgress = 100;
        _state = ProgressState.Success;
        NotifyStateChanged();
    }

    public void SetError(string message)
    {
        _currentMessage = message;
        _currentProgress = null;
        _state = ProgressState.Error;
        NotifyStateChanged();
    }

    public void Reset()
    {
        _currentMessage = string.Empty;
        _currentProgress = null;
        _state = ProgressState.Idle;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnProgressChanged?.Invoke();

    public void Dispose()
    {
        OnProgressChanged = null;
    }
}
