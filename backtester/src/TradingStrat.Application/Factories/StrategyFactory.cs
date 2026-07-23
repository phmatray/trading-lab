using System.Text.Json;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Application.Services;
using TradingStrat.Application.Strategies;
using TradingStrat.Domain.Entities;
using TradingStrat.Domain.Services;
using TradingStrat.Domain.Services.Indicators;
using TradingStrat.Domain.Strategies;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Factories;

public class StrategyFactory : IStrategyFactory
{
    private readonly IIndicatorCalculator _indicatorCalculator;
    private readonly IStrategyRegistry _registry;
    private readonly ICustomStrategyPort? _customStrategyPort;
    private readonly IMLPredictionService _mlPredictionService;
    private readonly StrategyParameterDefaults _parameterDefaults;
    private readonly IPythonExecutor _pythonExecutor;

    public StrategyFactory(
        IIndicatorCalculator indicatorCalculator,
        IStrategyRegistry registry,
        IMLPredictionService mlPredictionService,
        StrategyParameterDefaults parameterDefaults,
        IPythonExecutor pythonExecutor,
        ICustomStrategyPort? customStrategyPort = null)
    {
        _indicatorCalculator = indicatorCalculator;
        _registry = registry;
        _mlPredictionService = mlPredictionService;
        _parameterDefaults = parameterDefaults;
        _pythonExecutor = pythonExecutor;
        _customStrategyPort = customStrategyPort;
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
        int fastPeriod = GetParameterWithDefault<int>(
            parameters, "FastPeriod", StrategyType.MovingAverageCrossover);
        int slowPeriod = GetParameterWithDefault<int>(
            parameters, "SlowPeriod", StrategyType.MovingAverageCrossover);

        return new MovingAverageCrossoverStrategy(_indicatorCalculator, fastPeriod, slowPeriod);
    }

    private RSIStrategy CreateRSIStrategy(Dictionary<string, object> parameters)
    {
        int period = GetParameterWithDefault<int>(
            parameters, "Period", StrategyType.RSI);
        decimal oversoldThreshold = GetParameterWithDefault<decimal>(
            parameters, "OversoldThreshold", StrategyType.RSI);
        decimal overboughtThreshold = GetParameterWithDefault<decimal>(
            parameters, "OverboughtThreshold", StrategyType.RSI);

        return new RSIStrategy(_indicatorCalculator, period, oversoldThreshold, overboughtThreshold);
    }

    private MACDStrategy CreateMACDStrategy(Dictionary<string, object> parameters)
    {
        int fastPeriod = GetParameterWithDefault<int>(
            parameters, "FastPeriod", StrategyType.MACD);
        int slowPeriod = GetParameterWithDefault<int>(
            parameters, "SlowPeriod", StrategyType.MACD);
        int signalPeriod = GetParameterWithDefault<int>(
            parameters, "SignalPeriod", StrategyType.MACD);

        return new MACDStrategy(_indicatorCalculator, fastPeriod, slowPeriod, signalPeriod);
    }

    private MachineLearningStrategy CreateMachineLearningStrategy(Dictionary<string, object> parameters)
    {
        decimal buyThreshold = GetParameterWithDefault<decimal>(
            parameters, "BuyThreshold", StrategyType.MachineLearning);
        decimal sellThreshold = GetParameterWithDefault<decimal>(
            parameters, "SellThreshold", StrategyType.MachineLearning);
        var thresholds = new PredictionThresholds(buyThreshold, sellThreshold);

        return new MachineLearningStrategy(_indicatorCalculator, _mlPredictionService, thresholds);
    }

    private IchimokuStrategy CreateIchimokuStrategy(Dictionary<string, object> parameters)
    {
        int conversionLinePeriod = GetParameterWithDefault<int>(
            parameters, "ConversionLinePeriod", StrategyType.Ichimoku);
        int baseLinePeriod = GetParameterWithDefault<int>(
            parameters, "BaseLinePeriod", StrategyType.Ichimoku);
        int leadingSpanBPeriod = GetParameterWithDefault<int>(
            parameters, "LeadingSpanBPeriod", StrategyType.Ichimoku);
        int displacement = GetParameterWithDefault<int>(
            parameters, "Displacement", StrategyType.Ichimoku);

        IchimokuExitMode exitMode = GetParameter(parameters, "ExitMode", IchimokuExitMode.CloseBelowBaseLine);
        IchimokuEntryMode entryMode = GetParameter(parameters, "EntryMode", IchimokuEntryMode.AllConditionsOnly);

        int crossLookbackDays = GetParameterWithDefault<int>(
            parameters, "CrossLookbackDays", StrategyType.Ichimoku);
        decimal riskPercentage = GetParameterWithDefault<decimal>(
            parameters, "RiskPercentage", StrategyType.Ichimoku);

        TimeFrameAggregator timeframeAggregator = new();

        return new IchimokuStrategy(
            _indicatorCalculator,
            timeframeAggregator,
            conversionLinePeriod,
            baseLinePeriod,
            leadingSpanBPeriod,
            displacement,
            exitMode,
            entryMode,
            crossLookbackDays,
            riskPercentage);
    }

    /// <summary>
    /// Gets a parameter value with default from the StrategyParameterDefaults service.
    /// </summary>
    private T GetParameterWithDefault<T>(
        Dictionary<string, object> parameters,
        string key,
        StrategyType strategyType)
    {
        // Try to get from provided parameters first
        if (parameters.TryGetValue(key, out object? value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                // Fall through to default
            }
        }

        // Get default from centralized service
        return _parameterDefaults.GetDefault<T>(strategyType, key);
    }

    /// <summary>
    /// Legacy method for parameters not (yet) in StrategyParameterDefaults (e.g., enum types).
    /// </summary>
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

    /// <summary>
    /// Creates a custom strategy from a CustomStrategy entity.
    /// Supports both rule-based strategies (with StrategyDefinition) and Python strategies (with PythonCode).
    /// </summary>
    /// <param name="customStrategy">The custom strategy entity from the database.</param>
    /// <returns>A strategy ready for backtesting (CustomRuleBasedStrategy or PythonScriptStrategy).</returns>
    public IStrategy CreateCustomStrategy(CustomStrategy customStrategy)
    {
        return customStrategy.StrategyType switch
        {
            CustomStrategyType.RuleBased => CreateRuleBasedCustomStrategy(customStrategy),
            CustomStrategyType.Python => CreatePythonCustomStrategy(customStrategy),
            _ => throw new InvalidOperationException($"Unknown custom strategy type: {customStrategy.StrategyType}")
        };
    }

    private CustomRuleBasedStrategy CreateRuleBasedCustomStrategy(CustomStrategy customStrategy)
    {
        StrategyDefinition definition = JsonSerializer.Deserialize<StrategyDefinition>(
            customStrategy.DefinitionJson)
            ?? throw new InvalidOperationException("Failed to deserialize strategy definition");

        return new CustomRuleBasedStrategy(
            _indicatorCalculator,
            definition,
            customStrategy.Name,
            customStrategy.Description);
    }

    private PythonScriptStrategy CreatePythonCustomStrategy(CustomStrategy customStrategy)
    {
        if (string.IsNullOrWhiteSpace(customStrategy.PythonCode))
        {
            throw new InvalidOperationException($"Python strategy '{customStrategy.Name}' has no Python code");
        }

        return new PythonScriptStrategy(
            _indicatorCalculator,
            _pythonExecutor,
            customStrategy.PythonCode,
            customStrategy.Name,
            customStrategy.Description);
    }

    /// <summary>
    /// Creates a custom strategy by loading it from the database by ID.
    /// Requires ICustomStrategyPort to be injected in the constructor.
    /// </summary>
    /// <param name="customStrategyId">The ID of the custom strategy to load.</param>
    /// <returns>A CustomRuleBasedStrategy ready for backtesting.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the strategy is not found or repository not configured.</exception>
    public async Task<IStrategy> CreateCustomStrategyFromIdAsync(int customStrategyId)
    {
        if (_customStrategyPort is null)
        {
            throw new InvalidOperationException(
                "Cannot create custom strategy from ID: ICustomStrategyPort not configured in factory");
        }

        CustomStrategy? strategy = await _customStrategyPort.GetByIdAsync(customStrategyId);
        if (strategy is null)
        {
            throw new InvalidOperationException($"Custom strategy with ID {customStrategyId} not found");
        }

        // Increment usage count
        await _customStrategyPort.IncrementUsageCountAsync(customStrategyId);

        return CreateCustomStrategy(strategy);
    }
}
