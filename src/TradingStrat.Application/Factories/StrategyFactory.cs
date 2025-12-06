using Microsoft.Extensions.Logging;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Factories;

public class StrategyFactory : IStrategyFactory
{
    private readonly IIndicatorCalculator _indicatorCalculator;
    private readonly ILoggerFactory _loggerFactory;

    public StrategyFactory(
        IIndicatorCalculator indicatorCalculator,
        ILoggerFactory loggerFactory)
    {
        _indicatorCalculator = indicatorCalculator;
        _loggerFactory = loggerFactory;
    }

    public IStrategy CreateStrategy(string strategyType, Dictionary<string, object>? parameters = null)
    {
        parameters ??= new Dictionary<string, object>();

        return strategyType.ToLowerInvariant() switch
        {
            "ma" or "movingaverage" or "macrossover" => CreateMovingAverageCrossoverStrategy(parameters),
            "rsi" => CreateRSIStrategy(parameters),
            "macd" => CreateMACDStrategy(parameters),
            "ml" or "machinelearning" => CreateMachineLearningStrategy(parameters),
            _ => throw new ArgumentException($"Unknown strategy type: {strategyType}", nameof(strategyType))
        };
    }

    private MovingAverageCrossoverStrategy CreateMovingAverageCrossoverStrategy(Dictionary<string, object> parameters)
    {
        int fastPeriod = GetParameter(parameters, "FastPeriod", 20);
        int slowPeriod = GetParameter(parameters, "SlowPeriod", 50);

        return new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod, slowPeriod);
    }

    private RSIStrategy CreateRSIStrategy(Dictionary<string, object> parameters)
    {
        int period = GetParameter(parameters, "Period", 14);
        decimal oversoldThreshold = GetParameter<decimal>(parameters, "OversoldThreshold", 30);
        decimal overboughtThreshold = GetParameter<decimal>(parameters, "OverboughtThreshold", 70);

        return new RSIStrategy(_indicatorCalculator, period, oversoldThreshold, overboughtThreshold);
    }

    private MACDStrategy CreateMACDStrategy(Dictionary<string, object> parameters)
    {
        int fastPeriod = GetParameter(parameters, "FastPeriod", 12);
        int slowPeriod = GetParameter(parameters, "SlowPeriod", 26);
        int signalPeriod = GetParameter(parameters, "SignalPeriod", 9);

        return new MACDStrategy(_indicatorCalculator, fastPeriod, slowPeriod, signalPeriod);
    }

    private MachineLearningStrategy CreateMachineLearningStrategy(Dictionary<string, object> parameters)
    {
        decimal buyThreshold = GetParameter(parameters, "BuyThreshold", 0.01m);
        decimal sellThreshold = GetParameter(parameters, "SellThreshold", -0.01m);
        var thresholds = new PredictionThresholds(buyThreshold, sellThreshold);

        ILogger<MachineLearningStrategy> logger = _loggerFactory.CreateLogger<MachineLearningStrategy>();
        return new MachineLearningStrategy(_indicatorCalculator, thresholds, logger);
    }

    private T GetParameter<T>(Dictionary<string, object> parameters, string key, T defaultValue)
    {
        if (!parameters.TryGetValue(key, out object? value))
        {
            return defaultValue;
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
}
