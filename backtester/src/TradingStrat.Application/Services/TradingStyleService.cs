using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.Services;

/// <summary>
/// Application service for applying trading style defaults to backtest configurations
/// and adjusting strategy parameters based on timeframes.
/// Orchestrates domain logic from TradingStyle value object.
/// </summary>
public class TradingStyleService
{
    /// <summary>
    /// Applies trading style defaults to backtest configuration.
    /// User-provided values override style defaults where specified.
    /// </summary>
    /// <param name="config">Original backtest configuration.</param>
    /// <param name="tradingStyle">Trading style to apply defaults from.</param>
    /// <returns>New configuration with trading style defaults applied.</returns>
    /// <exception cref="InvalidOperationException">Thrown when timeframe is not valid for the trading style.</exception>
    public BacktestConfiguration ApplyDefaults(
        BacktestConfiguration config,
        TradingStyle tradingStyle)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(tradingStyle);

        // Use provided timeframe or fall back to trading style default
        TimeFrame timeFrame = config.TimeFrame;

        // Validate timeframe is appropriate for style
        if (!tradingStyle.IsTimeFrameValid(timeFrame))
        {
            throw new InvalidOperationException(
                $"TimeFrame {timeFrame} is not valid for {tradingStyle.Type} trading style. " +
                $"Valid range: {tradingStyle.MinTimeFrame} - {tradingStyle.MaxTimeFrame}");
        }

        // Apply style defaults where config uses generic defaults
        // Commission: Only override if config is using common default (0.001 = 0.1%)
        decimal commission = config.CommissionPercentage == 0.001m
            ? tradingStyle.DefaultCommissionPercentage
            : config.CommissionPercentage;

        // Minimum commission: Only override if config is using common default ($1.00)
        decimal minCommission = config.MinimumCommission == 1.0m
            ? tradingStyle.DefaultMinimumCommission
            : config.MinimumCommission;

        // Position sizing: Apply style defaults based on mode
        decimal? positionPercentage = config.PositionPercentage;
        if (config.PositionSizing == PositionSizingMode.Percentage && !positionPercentage.HasValue)
        {
            positionPercentage = tradingStyle.DefaultPositionSizePercent;
        }

        // Return new configuration with trading style applied
        return config with
        {
            CommissionPercentage = commission,
            MinimumCommission = minCommission,
            PositionPercentage = positionPercentage,
            TradingStyle = tradingStyle
        };
    }

    /// <summary>
    /// Adjusts strategy parameters based on trading style and timeframe.
    /// Scales period-based parameters to maintain similar time coverage across timeframes.
    /// </summary>
    /// <param name="parameters">Original strategy parameters.</param>
    /// <param name="tradingStyle">Trading style to use for adjustment.</param>
    /// <param name="currentTimeFrame">Timeframe being used for the backtest.</param>
    /// <param name="adjustForTimeFrame">Whether to apply timeframe adjustment (default: true).</param>
    /// <returns>New parameter dictionary with adjusted values.</returns>
    /// <example>
    /// For Swing Trading style (default H4) with 14-period RSI:
    /// - On H4 (default): Period = 14 bars (~2.3 days)
    /// - On H1 (4x smaller): Period = 56 bars (14 * 4) to maintain ~2.3 days coverage
    /// - On D1 (6x larger): Period = 2 bars (14 / 6, minimum 2) for ~2 days coverage
    /// </example>
    public Dictionary<string, object> AdjustStrategyParameters(
        Dictionary<string, object> parameters,
        TradingStyle tradingStyle,
        TimeFrame currentTimeFrame,
        bool adjustForTimeFrame = true)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(tradingStyle);
        ArgumentNullException.ThrowIfNull(currentTimeFrame);

        if (!adjustForTimeFrame)
        {
            return new Dictionary<string, object>(parameters);
        }

        var adjusted = new Dictionary<string, object>(parameters);

        // Adjust common period-based parameters
        // These parameter names are standard across RSI, MACD, MA Crossover strategies
        AdjustPeriodParameter(adjusted, "Period", tradingStyle, currentTimeFrame);
        AdjustPeriodParameter(adjusted, "FastPeriod", tradingStyle, currentTimeFrame);
        AdjustPeriodParameter(adjusted, "SlowPeriod", tradingStyle, currentTimeFrame);
        AdjustPeriodParameter(adjusted, "SignalPeriod", tradingStyle, currentTimeFrame);

        // Adjust oversold/overbought thresholds for RSI (remain unchanged - these are ratio-based)
        // No adjustment needed for Oversold/Overbought as they're independent of timeframe

        return adjusted;
    }

    /// <summary>
    /// Adjusts a single period parameter if it exists in the dictionary.
    /// </summary>
    private void AdjustPeriodParameter(
        Dictionary<string, object> parameters,
        string parameterName,
        TradingStyle tradingStyle,
        TimeFrame currentTimeFrame)
    {
        if (parameters.ContainsKey(parameterName))
        {
            int basePeriod = Convert.ToInt32(parameters[parameterName]);
            int adjustedPeriod = tradingStyle.AdjustPeriod(
                basePeriod,
                currentTimeFrame,
                adjustForTimeFrame: true);

            parameters[parameterName] = adjustedPeriod;
        }
    }

    /// <summary>
    /// Validates that a backtest configuration is compatible with a trading style.
    /// </summary>
    /// <param name="config">Backtest configuration to validate.</param>
    /// <param name="tradingStyle">Trading style to validate against.</param>
    /// <returns>True if configuration is valid for the trading style.</returns>
    public bool ValidateConfiguration(
        BacktestConfiguration config,
        TradingStyle tradingStyle)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(tradingStyle);

        // Check timeframe validity
        if (!tradingStyle.IsTimeFrameValid(config.TimeFrame))
        {
            return false;
        }

        // Check position size is within style limits
        if (config.PositionPercentage.HasValue)
        {
            if (config.PositionPercentage.Value > tradingStyle.MaxPositionSizePercent)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Gets recommended parameter adjustments for a strategy when switching timeframes.
    /// </summary>
    /// <param name="strategyType">Type of strategy (e.g., "rsi", "macd", "ma").</param>
    /// <param name="currentParameters">Current strategy parameters.</param>
    /// <param name="fromTimeFrame">Original timeframe.</param>
    /// <param name="toTimeFrame">Target timeframe.</param>
    /// <returns>Dictionary of recommended parameter changes.</returns>
    public Dictionary<string, int> GetRecommendedParameterChanges(
        string strategyType,
        Dictionary<string, object> currentParameters,
        TimeFrame fromTimeFrame,
        TimeFrame toTimeFrame)
    {
        ArgumentNullException.ThrowIfNull(strategyType);
        ArgumentNullException.ThrowIfNull(currentParameters);
        ArgumentNullException.ThrowIfNull(fromTimeFrame);
        ArgumentNullException.ThrowIfNull(toTimeFrame);

        var changes = new Dictionary<string, int>();

        int multiplier = toTimeFrame.ToMinutes() / fromTimeFrame.ToMinutes();

        // Calculate recommended changes for period parameters
        if (currentParameters.ContainsKey("Period"))
        {
            int currentPeriod = Convert.ToInt32(currentParameters["Period"]);
            int newPeriod = Math.Max(currentPeriod / multiplier, 2);
            changes["Period"] = newPeriod;
        }

        if (currentParameters.ContainsKey("FastPeriod"))
        {
            int currentPeriod = Convert.ToInt32(currentParameters["FastPeriod"]);
            int newPeriod = Math.Max(currentPeriod / multiplier, 2);
            changes["FastPeriod"] = newPeriod;
        }

        if (currentParameters.ContainsKey("SlowPeriod"))
        {
            int currentPeriod = Convert.ToInt32(currentParameters["SlowPeriod"]);
            int newPeriod = Math.Max(currentPeriod / multiplier, 2);
            changes["SlowPeriod"] = newPeriod;
        }

        return changes;
    }
}
