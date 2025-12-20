using System.ComponentModel.DataAnnotations;

namespace TradingStrat.Web.Models;

public class ComparisonFormModel
{
    [Required(ErrorMessage = "Ticker is required")]
    [MinLength(1, ErrorMessage = "Ticker must be at least 1 character")]
    public string Ticker { get; set; } = "CON3.L";

    [Range(100, 1000000, ErrorMessage = "Initial capital must be between $100 and $1,000,000")]
    public decimal InitialCapital { get; set; } = 10000m;

    [Required(ErrorMessage = "Variant A strategy type is required")]
    public string StrategyTypeA { get; set; } = "ma";

    public Dictionary<string, object> StrategyParametersA { get; set; } = new()
    {
        ["FastPeriod"] = 10,
        ["SlowPeriod"] = 30
    };

    [Required(ErrorMessage = "Variant B strategy type is required")]
    public string StrategyTypeB { get; set; } = "ma";

    public Dictionary<string, object> StrategyParametersB { get; set; } = new()
    {
        ["FastPeriod"] = 20,
        ["SlowPeriod"] = 50
    };

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
