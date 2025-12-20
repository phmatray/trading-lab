namespace TradingStrat.Web.Services;

public class ProgressService
{
    private string _currentMessage = string.Empty;
    private int? _currentProgress;
    private bool _isProcessing;

    public event Action? OnProgressChanged;

    public string CurrentMessage => _currentMessage;
    public int? CurrentProgress => _currentProgress;
    public bool IsProcessing => _isProcessing;

    public void UpdateProgress(string message, int? progress = null)
    {
        _currentMessage = message;
        _currentProgress = progress;
        _isProcessing = true;
        NotifyStateChanged();
    }

    public void Reset()
    {
        _currentMessage = string.Empty;
        _currentProgress = null;
        _isProcessing = false;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnProgressChanged?.Invoke();
}
