using System.ComponentModel.DataAnnotations;
using TradingStrat.Application.Configuration;
using TradingStrat.Domain.Strategies;
using TradingStrat.Web.Models.State;

namespace TradingStrat.Web.Models;

public class BacktestFormModel
{
    [Required(ErrorMessage = "Ticker is required")]
    [MinLength(1, ErrorMessage = "Ticker must be at least 1 character")]
    public string Ticker { get; set; } = "CON3.L";

    [Required(ErrorMessage = "Strategy type is required")]
    public StrategyType StrategyType { get; set; } = StrategyType.MovingAverageCrossover;

    /// <summary>
    /// Optional: If set, uses a custom strategy instead of built-in StrategyType.
    /// </summary>
    public int? CustomStrategyId { get; set; }

    public Dictionary<string, object> StrategyParameters { get; set; } = new()
    {
        ["FastPeriod"] = 20,
        ["SlowPeriod"] = 50
    };

    [Range(100, 1000000, ErrorMessage = "Initial capital must be between $100 and $1,000,000")]
    public decimal InitialCapital { get; set; } = 10000m;

    [Range(0, 0.1, ErrorMessage = "Commission must be between 0% and 10%")]
    public decimal CommissionPercentage { get; set; } = 0.001m;

    [Range(0, 100, ErrorMessage = "Minimum commission must be between $0 and $100")]
    public decimal MinimumCommission { get; set; } = 1.0m;

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public static BacktestFormModel FromPreferences(
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

        return new BacktestFormModel
        {
            Ticker = preferences.DefaultTicker,
            StrategyType = strategyType,
            StrategyParameters = new Dictionary<string, object>
            {
                ["FastPeriod"] = 20,
                ["SlowPeriod"] = 50
            },
            InitialCapital = preferences.BacktestDefaults.InitialCapital,
            CommissionPercentage = preferences.BacktestDefaults.CommissionPercentage,
            MinimumCommission = preferences.BacktestDefaults.MinimumCommission,
            StartDate = null,
            EndDate = null
        };
    }
}
