using TradingStrat.Domain.Strategies;

namespace TradingStrat.Application.Factories;

public interface IStrategyFactory
{
    IStrategy CreateStrategy(string strategyType, Dictionary<string, object>? parameters = null);
}
