using System.ComponentModel.DataAnnotations;

namespace TradingStrat.Web.Models;

public class DataFetchFormModel
{
    [Required(ErrorMessage = "Ticker is required")]
    [MinLength(1, ErrorMessage = "Ticker must be at least 1 character")]
    public string Ticker { get; set; } = "CON3.L";

    public string? ISIN { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}
