using TradingStrat.Web.Models;

namespace TradingStrat.Web.Services;

/// <summary>
/// Provides metadata about all available technical indicators.
/// This metadata is used to populate UI dropdowns and show parameter inputs.
/// </summary>
public class IndicatorMetadataService
{
    private readonly List<IndicatorMetadata> _indicators;

    public IndicatorMetadataService()
    {
        _indicators = InitializeIndicators();
    }

    /// <summary>
    /// Gets all available indicators grouped by category.
    /// </summary>
    public Dictionary<string, List<IndicatorMetadata>> GetIndicatorsByCategory()
    {
        return _indicators
            .GroupBy(i => i.Category)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.OrderBy(i => i.DisplayName).ToList());
    }

    /// <summary>
    /// Gets all available indicators as a flat list.
    /// </summary>
    public List<IndicatorMetadata> GetAllIndicators()
    {
        return _indicators.OrderBy(i => i.DisplayName).ToList();
    }

    /// <summary>
    /// Gets metadata for a specific indicator by name.
    /// </summary>
    public IndicatorMetadata? GetIndicator(string name)
    {
        return _indicators.FirstOrDefault(i =>
            i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets default parameters for an indicator.
    /// </summary>
    public Dictionary<string, object> GetDefaultParameters(string indicatorName)
    {
        IndicatorMetadata? indicator = GetIndicator(indicatorName);
        if (indicator is null)
        {
            return new Dictionary<string, object>();
        }

        return indicator.Parameters.ToDictionary(
            p => p.Name,
            p => p.DefaultValue
        );
    }

    private List<IndicatorMetadata> InitializeIndicators()
    {
        return
        [
            // Price-based indicators (5)
            new IndicatorMetadata
            {
                Name = "DailyReturn",
                DisplayName = "Daily Return",
                Description = "Daily percentage change in price",
                Category = "Price",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "LogReturn",
                DisplayName = "Log Return",
                Description = "Logarithmic return (more stable for large moves)",
                Category = "Price",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "HighLowRange",
                DisplayName = "High-Low Range",
                Description = "Difference between high and low prices",
                Category = "Price",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "OpenCloseRange",
                DisplayName = "Open-Close Range",
                Description = "Difference between open and close prices",
                Category = "Price",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "PricePosition",
                DisplayName = "Price Position",
                Description = "Position of close price within high-low range (0-1)",
                Category = "Price",
                Parameters = []
            },

            // Moving Averages (6)
            new IndicatorMetadata
            {
                Name = "SMA5",
                DisplayName = "SMA (5-day)",
                Description = "Simple Moving Average over 5 periods",
                Category = "Moving Average",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "SMA10",
                DisplayName = "SMA (10-day)",
                Description = "Simple Moving Average over 10 periods",
                Category = "Moving Average",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "SMA",
                DisplayName = "SMA (Custom Period)",
                Description = "Simple Moving Average with custom period",
                Category = "Moving Average",
                Parameters =
                [
                    new IndicatorParameter
                    {
                        Name = "Period",
                        DisplayName = "Period",
                        Type = "int",
                        DefaultValue = 20,
                        MinValue = 2,
                        MaxValue = 200
                    }
                ]
            },
            new IndicatorMetadata
            {
                Name = "EMA12",
                DisplayName = "EMA (12-day)",
                Description = "Exponential Moving Average over 12 periods",
                Category = "Moving Average",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "EMA26",
                DisplayName = "EMA (26-day)",
                Description = "Exponential Moving Average over 26 periods",
                Category = "Moving Average",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "PriceToSMA20Ratio",
                DisplayName = "Price/SMA20 Ratio",
                Description = "Current price divided by 20-day SMA",
                Category = "Moving Average",
                Parameters = []
            },

            // Momentum indicators (4)
            new IndicatorMetadata
            {
                Name = "RSI",
                DisplayName = "RSI (Relative Strength Index)",
                Description = "Momentum oscillator measuring overbought/oversold conditions",
                Category = "Momentum",
                Parameters =
                [
                    new IndicatorParameter
                    {
                        Name = "Period",
                        DisplayName = "Period",
                        Type = "int",
                        DefaultValue = 14,
                        MinValue = 2,
                        MaxValue = 100
                    }
                ]
            },
            new IndicatorMetadata
            {
                Name = "Momentum",
                DisplayName = "Momentum (5-day)",
                Description = "Rate of change over 5 periods",
                Category = "Momentum",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "ROC",
                DisplayName = "Rate of Change (10-day)",
                Description = "Percentage rate of change over 10 periods",
                Category = "Momentum",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "StochasticRSI",
                DisplayName = "Stochastic RSI",
                Description = "Stochastic calculation applied to RSI",
                Category = "Momentum",
                Parameters = []
            },

            // MACD indicators (3)
            new IndicatorMetadata
            {
                Name = "MACDLine",
                DisplayName = "MACD Line",
                Description = "Difference between 12-day and 26-day EMA",
                Category = "MACD",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "MACDSignal",
                DisplayName = "MACD Signal Line",
                Description = "9-day EMA of MACD line",
                Category = "MACD",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "MACDHistogram",
                DisplayName = "MACD Histogram",
                Description = "Difference between MACD line and signal line",
                Category = "MACD",
                Parameters = []
            },

            // Volatility indicators (4)
            new IndicatorMetadata
            {
                Name = "StdDev10",
                DisplayName = "Standard Deviation (10-day)",
                Description = "Price volatility over 10 periods",
                Category = "Volatility",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "StdDev20",
                DisplayName = "Standard Deviation (20-day)",
                Description = "Price volatility over 20 periods",
                Category = "Volatility",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "ATR",
                DisplayName = "Average True Range",
                Description = "Volatility indicator based on high-low-close range",
                Category = "Volatility",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "BollingerPosition",
                DisplayName = "Bollinger Band Position",
                Description = "Position of price within Bollinger Bands (0-1)",
                Category = "Volatility",
                Parameters = []
            },

            // Volume indicators (4)
            new IndicatorMetadata
            {
                Name = "VolumeChange",
                DisplayName = "Volume Change",
                Description = "Day-over-day volume change",
                Category = "Volume",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "VolumeMA10",
                DisplayName = "Volume MA (10-day)",
                Description = "10-day moving average of volume",
                Category = "Volume",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "VolumeRatio",
                DisplayName = "Volume Ratio",
                Description = "Current volume / 10-day average volume",
                Category = "Volume",
                Parameters = []
            },
            new IndicatorMetadata
            {
                Name = "PriceVolumeCorrelation",
                DisplayName = "Price-Volume Correlation",
                Description = "Correlation between price and volume changes",
                Category = "Volume",
                Parameters = []
            }
        ];
    }
}
