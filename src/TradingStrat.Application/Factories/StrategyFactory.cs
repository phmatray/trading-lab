using Microsoft.Extensions.Logging;
using TradingStrat.Application.Strategies;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Factories;

public class StrategyFactory : IStrategyFactory
{
    private readonly IIndicatorCalculator _indicatorCalculator;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IStrategyRegistry _registry;

    public StrategyFactory(
        IIndicatorCalculator indicatorCalculator,
        ILoggerFactory loggerFactory,
        IStrategyRegistry registry)
    {
        _indicatorCalculator = indicatorCalculator;
        _loggerFactory = loggerFactory;
        _registry = registry;
    }

    public IStrategy CreateStrategy(StrategyType strategyType, Dictionary<string, object>? parameters = null)
    {
        parameters ??= new Dictionary<string, object>();

        return strategyType switch
        {
            StrategyType.MovingAverageCrossover => CreateMovingAverageCrossoverStrategy(parameters),
            StrategyType.RSI => CreateRSIStrategy(parameters),
            StrategyType.MACD => CreateMACDStrategy(parameters),
            StrategyType.MachineLearning => CreateMachineLearningStrategy(parameters),
            StrategyType.Ichimoku => CreateIchimokuStrategy(parameters),
            _ => throw new ArgumentException($"Unknown strategy type: {strategyType}", nameof(strategyType))
        };
    }

    public string MapStrategyNameToType(string strategyName)
    {
        // Delegate to registry for parsing, fall back to "ma" if not recognized
        // This maintains backward compatibility with config files and user preferences
        if (_registry.TryParseStrategyType(strategyName, out StrategyType type))
        {
            StrategyDescriptor descriptor = _registry.GetDescriptor(type);
            return descriptor.Key;
        }

        // Default to MA if unrecognized (backward compatibility)
        return "ma";
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

    private IchimokuStrategy CreateIchimokuStrategy(Dictionary<string, object> parameters)
    {
        int tenkanPeriod = GetParameter(parameters, "TenkanPeriod", 9);
        int kijunPeriod = GetParameter(parameters, "KijunPeriod", 26);
        int senkouBPeriod = GetParameter(parameters, "SenkouBPeriod", 52);
        int displacement = GetParameter(parameters, "Displacement", 26);

        IchimokuExitMode exitMode = GetParameter(parameters, "ExitMode", IchimokuExitMode.CloseBelowKijun);
        IchimokuEntryMode entryMode = GetParameter(parameters, "EntryMode", IchimokuEntryMode.AllConditionsOnly);

        int crossLookbackDays = GetParameter(parameters, "CrossLookbackDays", 5);
        decimal riskPercentage = GetParameter(parameters, "RiskPercentage", 0.02m);

        TimeframeAggregator timeframeAggregator = new();

        return new IchimokuStrategy(
            _indicatorCalculator,
            timeframeAggregator,
            tenkanPeriod,
            kijunPeriod,
            senkouBPeriod,
            displacement,
            exitMode,
            entryMode,
            crossLookbackDays,
            riskPercentage);
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
