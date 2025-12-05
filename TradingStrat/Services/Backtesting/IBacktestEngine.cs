using TradingStrat.Models;
using TradingStrat.Services.Strategies;

namespace TradingStrat.Services.Backtesting;

public interface IBacktestEngine
{
    Task<BacktestResult> RunBacktestAsync(
        IStrategy strategy,
        BacktestConfiguration configuration,
        IProgress<(int current, int total, int trades)>? progress = null);
}
