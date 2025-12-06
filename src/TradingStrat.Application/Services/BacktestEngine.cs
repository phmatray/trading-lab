using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Services;

public class BacktestEngine
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly PerformanceCalculator _performanceCalculator;

    public BacktestEngine(
        IHistoricalDataPort historicalDataPort,
        PerformanceCalculator performanceCalculator)
    {
        _historicalDataPort = historicalDataPort;
        _performanceCalculator = performanceCalculator;
    }

    public async Task<BacktestResult> RunBacktestAsync(
        IStrategy strategy,
        BacktestConfiguration configuration,
        IProgress<(int current, int total, int trades)>? progress = null)
    {
        var filteredData = await LoadAndFilterData(configuration);
        strategy.Initialize(filteredData);

        var portfolio = InitializePortfolio(configuration.InitialCapital);
        var trades = new List<Trade>();
        var equityCurve = new List<EquityPoint>();

        ProcessBacktest(strategy, configuration, filteredData, portfolio, trades, equityCurve, progress);
        CloseFinalPosition(configuration, filteredData, portfolio, trades, equityCurve);

        var metrics = _performanceCalculator.Calculate(
            trades,
            equityCurve,
            configuration.InitialCapital,
            filteredData.Count);

        return CreateBacktestResult(strategy, configuration, trades, equityCurve, metrics);
    }

    private async Task<List<HistoricalPrice>> LoadAndFilterData(BacktestConfiguration configuration)
    {
        var historicalData = await _historicalDataPort.GetHistoricalDataAsync(configuration.Ticker);

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

        return filteredData;
    }

    private static PortfolioState InitializePortfolio(decimal initialCapital)
    {
        return new PortfolioState
        {
            Cash = initialCapital,
            Position = 0,
            AverageEntryPrice = 0,
            TotalCommissionPaid = 0
        };
    }

    private void ProcessBacktest(
        IStrategy strategy,
        BacktestConfiguration configuration,
        List<HistoricalPrice> filteredData,
        PortfolioState portfolio,
        List<Trade> trades,
        List<EquityPoint> equityCurve,
        IProgress<(int current, int total, int trades)>? progress)
    {
        for (int i = 0; i < filteredData.Count; i++)
        {
            var currentBar = filteredData[i];
            var currentPrice = currentBar.Close ?? 0;

            var signal = strategy.GenerateSignal(i, portfolio.Cash, portfolio.Position);

            ProcessSignal(signal, currentBar, portfolio, configuration, trades);

            var equity = portfolio.GetEquity(currentPrice);
            equityCurve.Add(new EquityPoint(currentBar.DateTime, equity, portfolio.Position));

            progress?.Report((i + 1, filteredData.Count, trades.Count));
        }
    }

    private void ProcessSignal(
        TradeSignal signal,
        HistoricalPrice currentBar,
        PortfolioState portfolio,
        BacktestConfiguration configuration,
        List<Trade> trades)
    {
        if (signal.Type == SignalType.Buy && signal.Quantity > 0)
        {
            ProcessBuySignal(signal, currentBar, portfolio, configuration, trades);
        }
        else if (signal.Type == SignalType.Sell && signal.Quantity > 0 && portfolio.Position >= signal.Quantity)
        {
            ProcessSellSignal(signal, currentBar, portfolio, configuration, trades);
        }
    }

    private void ProcessBuySignal(
        TradeSignal signal,
        HistoricalPrice currentBar,
        PortfolioState portfolio,
        BacktestConfiguration configuration,
        List<Trade> trades)
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

    private void ProcessSellSignal(
        TradeSignal signal,
        HistoricalPrice currentBar,
        PortfolioState portfolio,
        BacktestConfiguration configuration,
        List<Trade> trades)
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

    private void CloseFinalPosition(
        BacktestConfiguration configuration,
        List<HistoricalPrice> filteredData,
        PortfolioState portfolio,
        List<Trade> trades,
        List<EquityPoint> equityCurve)
    {
        if (portfolio.Position <= 0)
            return;

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

    private static BacktestResult CreateBacktestResult(
        IStrategy strategy,
        BacktestConfiguration configuration,
        List<Trade> trades,
        List<EquityPoint> equityCurve,
        PerformanceMetrics metrics)
    {
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
