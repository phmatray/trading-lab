using TradingStrat.Domain.Strategies;

namespace TradingStrat.Application.Strategies;

/// <summary>
/// Central registry of all strategy descriptors.
/// Provides type-safe access to strategy metadata and supports string parsing for backward compatibility.
/// Registered as singleton in DI - immutable metadata.
/// </summary>
public sealed class StrategyRegistry : IStrategyRegistry
{
    private readonly Dictionary<StrategyType, StrategyDescriptor> _descriptorsByType;
    private readonly Dictionary<string, StrategyType> _typesByKey;

    public StrategyRegistry()
    {
        // Initialize all strategy descriptors
        StrategyDescriptor[] descriptors =
        [
            CreateMovingAverageCrossoverDescriptor(),
            CreateRSIDescriptor(),
            CreateMACDDescriptor(),
            CreateMachineLearningDescriptor(),
            CreateIchimokuDescriptor()
        ];

        // Build lookup dictionaries for fast access
        _descriptorsByType = descriptors.ToDictionary(d => d.Type);
        _typesByKey = BuildKeyLookup(descriptors);
    }

    public IReadOnlyCollection<StrategyDescriptor> GetAll() =>
        _descriptorsByType.Values;

    public StrategyDescriptor GetDescriptor(StrategyType type)
    {
        if (!_descriptorsByType.TryGetValue(type, out StrategyDescriptor? descriptor))
        {
            throw new ArgumentException($"Unknown strategy type: {type}", nameof(type));
        }

        return descriptor;
    }

    public StrategyType ParseStrategyType(string strategyKey)
    {
        string normalized = strategyKey.Trim().ToLowerInvariant();

        if (_typesByKey.TryGetValue(normalized, out StrategyType type))
        {
            return type;
        }

        throw new ArgumentException(
            $"Unknown strategy key: '{strategyKey}'. Valid keys: {string.Join(", ", _typesByKey.Keys)}",
            nameof(strategyKey));
    }

    public bool TryParseStrategyType(string strategyKey, out StrategyType type)
    {
        string normalized = strategyKey.Trim().ToLowerInvariant();
        return _typesByKey.TryGetValue(normalized, out type);
    }

    private static Dictionary<string, StrategyType> BuildKeyLookup(StrategyDescriptor[] descriptors)
    {
        var lookup = new Dictionary<string, StrategyType>(StringComparer.OrdinalIgnoreCase);

        foreach (StrategyDescriptor descriptor in descriptors)
        {
            // Add primary key
            lookup[descriptor.Key] = descriptor.Type;

            // Add aliases
            foreach (string alias in descriptor.Aliases)
            {
                lookup[alias] = descriptor.Type;
            }
        }

        return lookup;
    }

    #region Descriptor Factory Methods

    private static StrategyDescriptor CreateMovingAverageCrossoverDescriptor() => new()
    {
        Type = StrategyType.MovingAverageCrossover,
        Key = "ma",
        DisplayName = "Moving Average Crossover",
        Description = "Buy when fast SMA crosses above slow SMA, sell when it crosses below.",
        Aliases = ["movingaverage", "macrossover"],
        Category = "Trend",
        Parameters = new Dictionary<string, ParameterSchema>
        {
            ["FastPeriod"] = new()
            {
                Name = "FastPeriod",
                ParameterType = typeof(int),
                DefaultValue = 20,
                DisplayName = "Fast Period",
                Description = "Period for fast moving average",
                MinValue = 1,
                MaxValue = 200
            },
            ["SlowPeriod"] = new()
            {
                Name = "SlowPeriod",
                ParameterType = typeof(int),
                DefaultValue = 50,
                DisplayName = "Slow Period",
                Description = "Period for slow moving average",
                MinValue = 1,
                MaxValue = 200
            }
        }
    };

    private static StrategyDescriptor CreateRSIDescriptor() => new()
    {
        Type = StrategyType.RSI,
        Key = "rsi",
        DisplayName = "RSI Strategy",
        Description = "Relative Strength Index mean-reversion strategy with overbought/oversold levels.",
        Aliases = [],
        Category = "Momentum",
        Parameters = new Dictionary<string, ParameterSchema>
        {
            ["Period"] = new()
            {
                Name = "Period",
                ParameterType = typeof(int),
                DefaultValue = 14,
                DisplayName = "RSI Period",
                Description = "Period for RSI calculation",
                MinValue = 1,
                MaxValue = 100
            },
            ["OversoldThreshold"] = new()
            {
                Name = "OversoldThreshold",
                ParameterType = typeof(decimal),
                DefaultValue = 30m,
                DisplayName = "Oversold Level",
                Description = "Buy signal threshold",
                MinValue = 0m,
                MaxValue = 100m
            },
            ["OverboughtThreshold"] = new()
            {
                Name = "OverboughtThreshold",
                ParameterType = typeof(decimal),
                DefaultValue = 70m,
                DisplayName = "Overbought Level",
                Description = "Sell signal threshold",
                MinValue = 0m,
                MaxValue = 100m
            }
        }
    };

    private static StrategyDescriptor CreateMACDDescriptor() => new()
    {
        Type = StrategyType.MACD,
        Key = "macd",
        DisplayName = "MACD Strategy",
        Description = "Moving Average Convergence Divergence crossover strategy.",
        Aliases = [],
        Category = "Momentum",
        Parameters = new Dictionary<string, ParameterSchema>
        {
            ["FastPeriod"] = new()
            {
                Name = "FastPeriod",
                ParameterType = typeof(int),
                DefaultValue = 12,
                DisplayName = "Fast EMA Period",
                Description = "Period for fast exponential moving average",
                MinValue = 1,
                MaxValue = 100
            },
            ["SlowPeriod"] = new()
            {
                Name = "SlowPeriod",
                ParameterType = typeof(int),
                DefaultValue = 26,
                DisplayName = "Slow EMA Period",
                Description = "Period for slow exponential moving average",
                MinValue = 1,
                MaxValue = 100
            },
            ["SignalPeriod"] = new()
            {
                Name = "SignalPeriod",
                ParameterType = typeof(int),
                DefaultValue = 9,
                DisplayName = "Signal Line Period",
                Description = "Period for MACD signal line",
                MinValue = 1,
                MaxValue = 100
            }
        }
    };

    private static StrategyDescriptor CreateMachineLearningDescriptor() => new()
    {
        Type = StrategyType.MachineLearning,
        Key = "ml",
        DisplayName = "ML FastTree",
        Description = "Machine learning strategy using FastTree gradient boosting with 26 technical indicators.",
        Aliases = ["machinelearning"],
        Category = "Machine Learning",
        Parameters = new Dictionary<string, ParameterSchema>
        {
            ["BuyThreshold"] = new()
            {
                Name = "BuyThreshold",
                ParameterType = typeof(decimal),
                DefaultValue = 0.01m,
                DisplayName = "Buy Threshold",
                Description = "Minimum predicted return to trigger buy (as decimal, e.g., 0.01 = 1%)",
                MinValue = 0m,
                MaxValue = 0.1m,
                Step = 0.001m
            },
            ["SellThreshold"] = new()
            {
                Name = "SellThreshold",
                ParameterType = typeof(decimal),
                DefaultValue = -0.01m,
                DisplayName = "Sell Threshold",
                Description = "Maximum predicted return to trigger sell (as decimal, e.g., -0.01 = -1%)",
                MinValue = -0.1m,
                MaxValue = 0m,
                Step = 0.001m
            }
        }
    };

    private static StrategyDescriptor CreateIchimokuDescriptor() => new()
    {
        Type = StrategyType.Ichimoku,
        Key = "ichimoku",
        DisplayName = "Ichimoku Cloud",
        Description = "Multi-timeframe Ichimoku strategy with weekly trend filter and risk-based position sizing.",
        Aliases = ["ichi"],
        Category = "Trend",
        Parameters = new Dictionary<string, ParameterSchema>
        {
            ["TenkanPeriod"] = new()
            {
                Name = "TenkanPeriod",
                ParameterType = typeof(int),
                DefaultValue = 9,
                DisplayName = "Tenkan Period",
                Description = "Period for Tenkan-sen (conversion line)",
                MinValue = 1,
                MaxValue = 100
            },
            ["KijunPeriod"] = new()
            {
                Name = "KijunPeriod",
                ParameterType = typeof(int),
                DefaultValue = 26,
                DisplayName = "Kijun Period",
                Description = "Period for Kijun-sen (base line)",
                MinValue = 1,
                MaxValue = 100
            },
            ["SenkouBPeriod"] = new()
            {
                Name = "SenkouBPeriod",
                ParameterType = typeof(int),
                DefaultValue = 52,
                DisplayName = "Senkou B Period",
                Description = "Period for Senkou Span B (cloud boundary)",
                MinValue = 1,
                MaxValue = 200
            },
            ["Displacement"] = new()
            {
                Name = "Displacement",
                ParameterType = typeof(int),
                DefaultValue = 26,
                DisplayName = "Displacement",
                Description = "Chikou Span displacement (days shifted)",
                MinValue = 1,
                MaxValue = 100
            },
            ["ExitMode"] = new()
            {
                Name = "ExitMode",
                ParameterType = typeof(string),
                DefaultValue = "CloseBelowKijun",
                DisplayName = "Exit Mode",
                Description = "Exit signal mode (CloseBelowKijun, PriceBelowCloud, TenkanBelowKijun)"
            },
            ["EntryMode"] = new()
            {
                Name = "EntryMode",
                ParameterType = typeof(string),
                DefaultValue = "AllConditionsOnly",
                DisplayName = "Entry Mode",
                Description = "Entry signal mode (AllConditionsOnly, ChikouOnly, TenkanKijunOnly)"
            },
            ["CrossLookbackDays"] = new()
            {
                Name = "CrossLookbackDays",
                ParameterType = typeof(int),
                DefaultValue = 5,
                DisplayName = "Cross Lookback Days",
                Description = "Number of days to look back for crossover signals",
                MinValue = 1,
                MaxValue = 20
            },
            ["RiskPercentage"] = new()
            {
                Name = "RiskPercentage",
                ParameterType = typeof(decimal),
                DefaultValue = 0.02m,
                DisplayName = "Risk Per Trade",
                Description = "Risk percentage per trade (as decimal, e.g., 0.02 = 2%)",
                MinValue = 0.001m,
                MaxValue = 0.1m,
                Step = 0.001m
            }
        }
    };

    #endregion
}
