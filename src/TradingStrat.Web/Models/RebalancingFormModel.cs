using System.ComponentModel.DataAnnotations;

namespace TradingStrat.Web.Models;

/// <summary>
/// Individual target allocation for a ticker.
/// </summary>
public class TargetAllocationModel
{
    /// <summary>
    /// Gets or sets the ticker symbol.
    /// </summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target allocation percentage.
    /// </summary>
    public decimal Percentage { get; set; } = 0m;
}

/// <summary>
/// Form model for portfolio rebalancing.
/// </summary>
public class RebalancingFormModel
{
    /// <summary>
    /// Gets or sets the portfolio ID.
    /// </summary>
    [Required(ErrorMessage = "Portfolio ID is required")]
    public int PortfolioId { get; set; }

    /// <summary>
    /// Gets or sets the target allocations for each ticker (percentage).
    /// </summary>
    public List<TargetAllocationModel> TargetAllocations { get; set; } = new();

    /// <summary>
    /// Gets or sets the target cash percentage.
    /// </summary>
    [Range(0, 100, ErrorMessage = "Cash percentage must be between 0% and 100%")]
    public decimal CashPercentage { get; set; } = 0m;

    /// <summary>
    /// Gets or sets the commission percentage per trade.
    /// </summary>
    [Range(0, 10, ErrorMessage = "Commission percentage must be between 0% and 10%")]
    public decimal CommissionPercentage { get; set; } = 0.1m;

    /// <summary>
    /// Gets or sets the minimum commission per trade.
    /// </summary>
    [Range(0, 100, ErrorMessage = "Minimum commission must be between $0 and $100")]
    public decimal MinimumCommission { get; set; } = 1.0m;

    /// <summary>
    /// Validates that allocations (including cash) sum to 100%.
    /// </summary>
    /// <returns>True if allocations are valid, false otherwise.</returns>
    public bool ValidateAllocations()
    {
        if (TargetAllocations is null || TargetAllocations.Count == 0)
        {
            return CashPercentage == 100m;
        }

        decimal totalAllocation = TargetAllocations
            .Where(a => !string.IsNullOrWhiteSpace(a.Ticker))
            .Sum(a => a.Percentage) + CashPercentage;

        // Allow small rounding errors (0.01%)
        return Math.Abs(totalAllocation - 100m) < 0.01m;
    }
}
