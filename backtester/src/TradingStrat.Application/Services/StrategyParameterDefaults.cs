using TradingStrat.Domain.Strategies;

namespace TradingStrat.Application.Services;

/// <summary>
/// Centralized service providing default parameter values, ranges, and metadata for all built-in strategies.
/// Single source of truth for strategy parameter configuration.
/// </summary>
public class StrategyParameterDefaults
{
    private readonly Dictionary<StrategyType, StrategyParameters> _strategyParameters;

    public StrategyParameterDefaults()
    {
        _strategyParameters = InitializeStrategyParameters();
    }

    /// <summary>
    /// Gets the parameter metadata for a specific strategy.
    /// </summary>
    public StrategyParameters GetParameters(StrategyType strategyType)
    {
        if (!_strategyParameters.TryGetValue(strategyType, out StrategyParameters? parameters))
        {
            throw new ArgumentException($"Unknown strategy type: {strategyType}", nameof(strategyType));
        }

        return parameters;
    }

    /// <summary>
    /// Gets the default value for a specific parameter of a strategy.
    /// </summary>
    public T GetDefault<T>(StrategyType strategyType, string parameterName)
    {
        StrategyParameters parameters = GetParameters(strategyType);
        ParameterMetadata param = parameters.Get(parameterName);
        return param.GetDefault<T>();
    }

    /// <summary>
    /// Gets all default parameters for a strategy as a dictionary.
    /// </summary>
    public Dictionary<string, object> GetAllDefaults(StrategyType strategyType)
    {
        StrategyParameters parameters = GetParameters(strategyType);
        return parameters.GetDefaults();
    }

    /// <summary>
    /// Gets the min/max range for a numeric parameter.
    /// </summary>
    public (T Min, T Max) GetRange<T>(StrategyType strategyType, string parameterName) where T : struct
    {
        StrategyParameters parameters = GetParameters(strategyType);
        ParameterMetadata param = parameters.Get(parameterName);
        return param.GetRange<T>();
    }

    /// <summary>
    /// Tries to get parameter metadata. Returns false if parameter doesn't exist.
    /// </summary>
    public bool TryGetParameter(
        StrategyType strategyType,
        string parameterName,
        out ParameterMetadata? parameter)
    {
        parameter = null;

        if (!_strategyParameters.TryGetValue(strategyType, out StrategyParameters? parameters))
        {
            return false;
        }

        return parameters.TryGet(parameterName, out parameter);
    }

    private Dictionary<StrategyType, StrategyParameters> InitializeStrategyParameters()
    {
        return new Dictionary<StrategyType, StrategyParameters>
        {
            [StrategyType.MovingAverageCrossover] = CreateMovingAverageCrossoverParameters(),
            [StrategyType.RSI] = CreateRSIParameters(),
            [StrategyType.MACD] = CreateMACDParameters(),
            [StrategyType.MachineLearning] = CreateMachineLearningParameters(),
            [StrategyType.Ichimoku] = CreateIchimokuParameters()
        };
    }

    private StrategyParameters CreateMovingAverageCrossoverParameters()
    {
        StrategyParameters parameters = new();

        parameters.Add(new ParameterMetadata
        {
            Name = "FastPeriod",
            DisplayName = "Fast Period",
            Description = "Period for the fast moving average",
            Type = "int",
            DefaultValue = 20,
            MinValue = 2,
            MaxValue = 100
        });

        parameters.Add(new ParameterMetadata
        {
            Name = "SlowPeriod",
            DisplayName = "Slow Period",
            Description = "Period for the slow moving average",
            Type = "int",
            DefaultValue = 50,
            MinValue = 10,
            MaxValue = 200
        });

        return parameters;
    }

    private StrategyParameters CreateRSIParameters()
    {
        StrategyParameters parameters = new();

        parameters.Add(new ParameterMetadata
        {
            Name = "Period",
            DisplayName = "RSI Period",
            Description = "Number of periods for RSI calculation",
            Type = "int",
            DefaultValue = 14,
            MinValue = 2,
            MaxValue = 100
        });

        parameters.Add(new ParameterMetadata
        {
            Name = "OversoldThreshold",
            DisplayName = "Oversold Threshold",
            Description = "RSI value below which the asset is considered oversold (buy signal)",
            Type = "decimal",
            DefaultValue = 30m,
            MinValue = 0m,
            MaxValue = 50m
        });

        parameters.Add(new ParameterMetadata
        {
            Name = "OverboughtThreshold",
            DisplayName = "Overbought Threshold",
            Description = "RSI value above which the asset is considered overbought (sell signal)",
            Type = "decimal",
            DefaultValue = 70m,
            MinValue = 50m,
            MaxValue = 100m
        });

        return parameters;
    }

    private StrategyParameters CreateMACDParameters()
    {
        StrategyParameters parameters = new();

        parameters.Add(new ParameterMetadata
        {
            Name = "FastPeriod",
            DisplayName = "Fast EMA Period",
            Description = "Period for the fast exponential moving average",
            Type = "int",
            DefaultValue = 12,
            MinValue = 2,
            MaxValue = 50
        });

        parameters.Add(new ParameterMetadata
        {
            Name = "SlowPeriod",
            DisplayName = "Slow EMA Period",
            Description = "Period for the slow exponential moving average",
            Type = "int",
            DefaultValue = 26,
            MinValue = 10,
            MaxValue = 100
        });

        parameters.Add(new ParameterMetadata
        {
            Name = "SignalPeriod",
            DisplayName = "Signal Line Period",
            Description = "Period for the signal line (EMA of MACD)",
            Type = "int",
            DefaultValue = 9,
            MinValue = 2,
            MaxValue = 50
        });

        return parameters;
    }

    private StrategyParameters CreateMachineLearningParameters()
    {
        StrategyParameters parameters = new();

        parameters.Add(new ParameterMetadata
        {
            Name = "BuyThreshold",
            DisplayName = "Buy Threshold",
            Description = "Minimum predicted return to trigger a buy signal",
            Type = "decimal",
            DefaultValue = 0.01m,
            MinValue = 0.001m,
            MaxValue = 0.1m
        });

        parameters.Add(new ParameterMetadata
        {
            Name = "SellThreshold",
            DisplayName = "Sell Threshold",
            Description = "Maximum predicted return to trigger a sell signal (negative value)",
            Type = "decimal",
            DefaultValue = -0.01m,
            MinValue = -0.1m,
            MaxValue = -0.001m
        });

        return parameters;
    }

    private StrategyParameters CreateIchimokuParameters()
    {
        StrategyParameters parameters = new();

        parameters.Add(new ParameterMetadata
        {
            Name = "ConversionLinePeriod",
            DisplayName = "Conversion Line Period (Tenkan-sen)",
            Description = "Period for the conversion line calculation",
            Type = "int",
            DefaultValue = 9,
            MinValue = 5,
            MaxValue = 20
        });

        parameters.Add(new ParameterMetadata
        {
            Name = "BaseLinePeriod",
            DisplayName = "Base Line Period (Kijun-sen)",
            Description = "Period for the base line calculation",
            Type = "int",
            DefaultValue = 26,
            MinValue = 10,
            MaxValue = 50
        });

        parameters.Add(new ParameterMetadata
        {
            Name = "LeadingSpanBPeriod",
            DisplayName = "Leading Span B Period (Senkou Span B)",
            Description = "Period for the leading span B calculation",
            Type = "int",
            DefaultValue = 52,
            MinValue = 26,
            MaxValue = 100
        });

        parameters.Add(new ParameterMetadata
        {
            Name = "Displacement",
            DisplayName = "Cloud Displacement",
            Description = "Number of periods to shift the cloud forward",
            Type = "int",
            DefaultValue = 26,
            MinValue = 10,
            MaxValue = 50
        });

        parameters.Add(new ParameterMetadata
        {
            Name = "CrossLookbackDays",
            DisplayName = "Cross Lookback Days",
            Description = "Number of days to look back for crossover detection",
            Type = "int",
            DefaultValue = 5,
            MinValue = 1,
            MaxValue = 20
        });

        parameters.Add(new ParameterMetadata
        {
            Name = "RiskPercentage",
            DisplayName = "Risk Percentage",
            Description = "Percentage of capital to risk per trade",
            Type = "decimal",
            DefaultValue = 0.02m,
            MinValue = 0.001m,
            MaxValue = 0.1m
        });

        return parameters;
    }
}
