using TradingStrat.Models;

namespace TradingStrat.Services.Strategies;

public interface IStrategy
{
    string Name { get; }
    string Description { get; }

    void Initialize(IReadOnlyList<HistoricalPrice> historicalData);

    TradeSignal GenerateSignal(int currentIndex, decimal currentCash, int currentPosition);

    Dictionary<string, object> GetParameters();
}
