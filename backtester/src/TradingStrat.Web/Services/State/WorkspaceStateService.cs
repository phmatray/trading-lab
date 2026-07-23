using TradingStrat.Domain.Entities;

namespace TradingStrat.Web.Services.State;

/// <summary>
/// State management service for the unified Strategy Workspace.
/// Preserves context across Define → Test → Optimize → Deploy tabs.
/// </summary>
public class WorkspaceStateService
{
    private WorkspaceState _state = new();

    /// <summary>
    /// Event raised when workspace state changes
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// Gets the current workspace state
    /// </summary>
    public WorkspaceState State => _state;

    /// <summary>
    /// Sets the current custom strategy being worked on
    /// </summary>
    public void SetCurrentStrategy(CustomStrategy? strategy)
    {
        _state.CurrentStrategy = strategy;
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the backtest configuration
    /// </summary>
    public void SetTestConfig(WorkspaceBacktestConfig? config)
    {
        _state.TestConfig = config;
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the backtest result
    /// </summary>
    public void SetTestResult(BacktestResult? result)
    {
        _state.TestResult = result;
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the optimization configuration
    /// </summary>
    public void SetOptimizationConfig(WorkspaceOptimizationConfig? config)
    {
        _state.OptimizeConfig = config;
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the optimization result
    /// </summary>
    public void SetOptimizationResult(WorkspaceOptimizationResult? result)
    {
        _state.OptimizeResult = result;
        NotifyStateChanged();
    }

    /// <summary>
    /// Sets the active tab index (0 = Define, 1 = Test, 2 = Optimize, 3 = Deploy)
    /// </summary>
    public void SetActiveTab(int tabIndex)
    {
        _state.ActiveTab = tabIndex;
        NotifyStateChanged();
    }

    /// <summary>
    /// Clears all workspace state (start fresh)
    /// </summary>
    public void Clear()
    {
        _state = new WorkspaceState();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Workspace state container
/// </summary>
public class WorkspaceState
{
    public CustomStrategy? CurrentStrategy { get; set; }
    public WorkspaceBacktestConfig? TestConfig { get; set; }
    public BacktestResult? TestResult { get; set; }
    public WorkspaceOptimizationConfig? OptimizeConfig { get; set; }
    public WorkspaceOptimizationResult? OptimizeResult { get; set; }
    public int ActiveTab { get; set; }
}

/// <summary>
/// Simplified backtest config for workspace
/// </summary>
public class WorkspaceBacktestConfig
{
    public string Ticker { get; set; } = "AAPL";
    public DateTime StartDate { get; set; } = DateTime.Today.AddYears(-2);
    public DateTime EndDate { get; set; } = DateTime.Today;
    public decimal InitialCapital { get; set; } = 10000m;
    public decimal CommissionPercentage { get; set; } = 0.1m;
    public decimal MinimumCommission { get; set; } = 1.0m;
}

/// <summary>
/// Simplified optimization config for workspace
/// </summary>
public class WorkspaceOptimizationConfig
{
    public string Ticker { get; set; } = "AAPL";
    public DateTime StartDate { get; set; } = DateTime.Today.AddYears(-2);
    public DateTime EndDate { get; set; } = DateTime.Today;
    public decimal InitialCapital { get; set; } = 10000m;
    public string OptimizationType { get; set; } = "GridSearch";
    public Dictionary<string, ParameterRangeModel> ParameterRanges { get; set; } = new();
}

/// <summary>
/// Parameter range for optimization
/// </summary>
public class ParameterRangeModel
{
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal Step { get; set; }
}

/// <summary>
/// Simplified optimization result for workspace
/// </summary>
public class WorkspaceOptimizationResult
{
    public Dictionary<string, object> BestParameters { get; set; } = new();
    public decimal BestScore { get; set; }
    public string Objective { get; set; } = string.Empty;
    public int TotalRuns { get; set; }
}
