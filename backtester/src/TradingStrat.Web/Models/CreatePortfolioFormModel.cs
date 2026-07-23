using System.ComponentModel.DataAnnotations;

namespace TradingStrat.Web.Models;

/// <summary>
/// Form model for creating a new portfolio.
/// </summary>
public class CreatePortfolioFormModel
{
    /// <summary>
    /// Gets or sets the portfolio name.
    /// </summary>
    [Required(ErrorMessage = "Portfolio name is required")]
    [MinLength(3, ErrorMessage = "Portfolio name must be at least 3 characters")]
    [MaxLength(100, ErrorMessage = "Portfolio name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the portfolio description.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the initial cash amount for the portfolio.
    /// </summary>
    [Range(0, 10000000, ErrorMessage = "Initial cash must be between $0 and $10,000,000")]
    public decimal InitialCash { get; set; } = 10000m;
}
