using TradingStrat.Domain.Entities;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Web.Models.State;

/// <summary>
/// Application-wide state for context preservation across pages.
/// </summary>
public class AppState
{
    public string? CurrentTicker { get; set; }
    public string? CurrentStrategyType { get; set; }
    public Dictionary<string, object> CurrentStrategyParameters { get; set; } = new();
    public NavigationState NavigationState { get; set; } = new();

    // NEW: Context preservation for seamless workflows
    public BacktestContext? LastBacktestContext { get; set; }
    public OptimizationContext? LastOptimizationContext { get; set; }
    public List<string> RecentTickers { get; set; } = new(); // Max 10 recent tickers
}

public class NavigationState
{
    public string? LastVisitedPage { get; set; }
    public DateTime LastNavigationTime { get; set; }
}

/// <summary>
/// Context from the last backtest execution.
/// Enables "Create Portfolio from Backtest" and "Compare with Others" quick actions.
/// </summary>
public class BacktestContext
{
    public int? BacktestRunId { get; set; }
    public string Ticker { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public Dictionary<string, object> StrategyParameters { get; set; } = new();
    public BacktestConfig? Config { get; set; }
    public BacktestResult? Result { get; set; }
}

/// <summary>
/// Context from the last optimization execution.
/// Enables "Apply Best Parameters" quick action.
/// </summary>
public class OptimizationContext
{
    public int? CustomStrategyId { get; set; }
    public Dictionary<string, decimal> BestParameters { get; set; } = new();
    public decimal BestObjectiveValue { get; set; }
    public string OptimizationAlgorithm { get; set; } = string.Empty;
}
