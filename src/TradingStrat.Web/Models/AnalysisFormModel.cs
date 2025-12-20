using System.ComponentModel.DataAnnotations;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Web.Models;

public class AnalysisFormModel
{
    [Required(ErrorMessage = "Ticker is required")]
    [MinLength(1, ErrorMessage = "Ticker must be at least 1 character")]
    public string Ticker { get; set; } = "CON3.L";

    public bool FetchFreshData { get; set; } = true;

    [Range(0.1, 10, ErrorMessage = "Buy threshold must be between 0.1% and 10%")]
    public decimal BuyThreshold { get; set; } = 1.0m;

    [Range(-10, -0.1, ErrorMessage = "Sell threshold must be between -10% and -0.1%")]
    public decimal SellThreshold { get; set; } = -1.0m;

    public PredictionThresholds GetThresholds()
    {
        return new PredictionThresholds
        {
            BuyThreshold = BuyThreshold / 100m,
            SellThreshold = SellThreshold / 100m
        };
    }
}
