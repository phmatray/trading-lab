using TradingStrat.Models;
using TradingStrat.Services.Strategies;

namespace TradingStrat.Services.Backtesting;

public class BacktestEngine : IBacktestEngine
{
    private readonly IDataRepository _dataRepository;

    public BacktestEngine(IDataRepository dataRepository)
    {
        _dataRepository = dataRepository;
    }

    public async Task<BacktestResult> RunBacktestAsync(
        IStrategy strategy,
        BacktestConfiguration configuration,
        IProgress<(int current, int total, int trades)>? progress = null)
    {
        var historicalData = await _dataRepository.GetHistoricalDataAsync(configuration.Ticker);

        var filteredData = historicalData
            .Where(h => h.DateTime >= configuration.StartDate && h.DateTime <= configuration.EndDate)
            .OrderBy(h => h.DateTime)
            .ToList();

        if (filteredData.Count == 0)
        {
            throw new InvalidOperationException(
                $"No historical data found for {configuration.Ticker} " +
                $"between {configuration.StartDate:yyyy-MM-dd} and {configuration.EndDate:yyyy-MM-dd}");
        }

        strategy.Initialize(filteredData);

        var portfolio = new PortfolioState
        {
            Cash = configuration.InitialCapital,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };

        var trades = new List<Trade>();
        var equityCurve = new List<EquityPoint>();

        for (int i = 0; i < filteredData.Count; i++)
        {
            var currentBar = filteredData[i];
            var currentPrice = currentBar.Close ?? 0;

            var signal = strategy.GenerateSignal(i, portfolio.Cash, portfolio.Position);

            if (signal.Type == SignalType.Buy && signal.Quantity > 0)
            {
                var commission = CalculateCommission(
                    signal.Quantity * signal.Price,
                    configuration.CommissionPercentage,
                    configuration.MinimumCommission);

                var grossAmount = signal.Quantity * signal.Price;
                var netAmount = grossAmount + commission;

                if (netAmount <= portfolio.Cash)
                {
                    portfolio.ExecuteBuy(signal.Quantity, signal.Price, commission);

                    trades.Add(new Trade
                    {
                        DateTime = currentBar.DateTime,
                        Type = TradeType.Buy,
                        Price = signal.Price,
                        Quantity = signal.Quantity,
                        Commission = commission,
                        GrossAmount = grossAmount,
                        NetAmount = netAmount,
                        Reason = signal.Reason
                    });
                }
            }
            else if (signal.Type == SignalType.Sell && signal.Quantity > 0 && portfolio.Position >= signal.Quantity)
            {
                var commission = CalculateCommission(
                    signal.Quantity * signal.Price,
                    configuration.CommissionPercentage,
                    configuration.MinimumCommission);

                var grossAmount = signal.Quantity * signal.Price;
                var netAmount = grossAmount - commission;
                var profitLoss = (signal.Price - portfolio.AverageEntryPrice) * signal.Quantity - commission;

                portfolio.ExecuteSell(signal.Quantity, signal.Price, commission);

                trades.Add(new Trade
                {
                    DateTime = currentBar.DateTime,
                    Type = TradeType.Sell,
                    Price = signal.Price,
                    Quantity = signal.Quantity,
                    Commission = commission,
                    GrossAmount = grossAmount,
                    NetAmount = netAmount,
                    Reason = signal.Reason,
                    ProfitLoss = profitLoss
                });
            }

            var equity = portfolio.GetEquity(currentPrice);
            equityCurve.Add(new EquityPoint(currentBar.DateTime, equity, portfolio.Position));

            progress?.Report((i + 1, filteredData.Count, trades.Count));
        }

        if (portfolio.Position > 0)
        {
            var lastBar = filteredData[^1];
            var lastPrice = lastBar.Close ?? 0;
            var commission = CalculateCommission(
                portfolio.Position * lastPrice,
                configuration.CommissionPercentage,
                configuration.MinimumCommission);

            var grossAmount = portfolio.Position * lastPrice;
            var netAmount = grossAmount - commission;
            var profitLoss = (lastPrice - portfolio.AverageEntryPrice) * portfolio.Position - commission;

            portfolio.ExecuteSell(portfolio.Position, lastPrice, commission);

            trades.Add(new Trade
            {
                DateTime = lastBar.DateTime,
                Type = TradeType.Sell,
                Price = lastPrice,
                Quantity = portfolio.Position,
                Commission = commission,
                GrossAmount = grossAmount,
                NetAmount = netAmount,
                Reason = "Close final position",
                ProfitLoss = profitLoss
            });

            var finalEquity = portfolio.GetEquity(lastPrice);
            equityCurve[^1] = new EquityPoint(lastBar.DateTime, finalEquity, 0);
        }

        var calculator = new PerformanceCalculator();
        var metrics = calculator.Calculate(
            trades,
            equityCurve,
            configuration.InitialCapital,
            filteredData.Count);

        return new BacktestResult(
            strategy.Name,
            strategy.Description,
            strategy.GetParameters(),
            configuration.Ticker,
            configuration.StartDate,
            configuration.EndDate,
            configuration.InitialCapital,
            configuration.CommissionPercentage,
            configuration.MinimumCommission,
            trades,
            equityCurve,
            metrics
        );
    }

    private decimal CalculateCommission(decimal amount, decimal percentage, decimal minimum)
    {
        var commission = amount * percentage;
        return Math.Max(commission, minimum);
    }
}
