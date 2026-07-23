using System.ComponentModel.DataAnnotations;
using TradingStrat.Application.Configuration;
using TradingStrat.Domain.Strategies;
using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Models;

public class ComparisonFormModel
{
    [Required(ErrorMessage = "Ticker is required")]
    [MinLength(1, ErrorMessage = "Ticker must be at least 1 character")]
    public string Ticker { get; set; } = "CON3.L";

    [Range(100, 1000000, ErrorMessage = "Initial capital must be between $100 and $1,000,000")]
    public decimal InitialCapital { get; set; } = 10000m;

    [Required(ErrorMessage = "Variant A strategy type is required")]
    public StrategyType StrategyTypeA { get; set; } = StrategyType.MovingAverageCrossover;

    public Dictionary<string, object> StrategyParametersA { get; set; } = new()
    {
        ["FastPeriod"] = 10,
        ["SlowPeriod"] = 30
    };

    [Required(ErrorMessage = "Variant B strategy type is required")]
    public StrategyType StrategyTypeB { get; set; } = StrategyType.MovingAverageCrossover;

    public Dictionary<string, object> StrategyParametersB { get; set; } = new()
    {
        ["FastPeriod"] = 20,
        ["SlowPeriod"] = 50
    };

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public static ComparisonFormModel FromPreferences(
        UserPreferences preferences,
        TradingConfiguration config,
        Application.Strategies.IStrategyRegistry registry)
    {
        // Parse strategy type from string preference
        StrategyType strategyType = StrategyType.MovingAverageCrossover; // Default fallback
        if (!string.IsNullOrEmpty(preferences.BacktestDefaults.PreferredStrategy))
        {
            registry.TryParseStrategyType(preferences.BacktestDefaults.PreferredStrategy, out strategyType);
        }

        return new ComparisonFormModel
        {
            Ticker = preferences.DefaultTicker,
            InitialCapital = preferences.BacktestDefaults.InitialCapital,
            StrategyTypeA = strategyType,
            StrategyParametersA = new Dictionary<string, object>
            {
                ["FastPeriod"] = 10,
                ["SlowPeriod"] = 30
            },
            StrategyTypeB = strategyType,
            StrategyParametersB = new Dictionary<string, object>
            {
                ["FastPeriod"] = 20,
                ["SlowPeriod"] = 50
            },
            StartDate = null,
            EndDate = null
        };
    }
}
