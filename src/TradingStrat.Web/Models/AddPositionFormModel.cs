using System.ComponentModel.DataAnnotations;

namespace TradingStrat.Web.Models;

/// <summary>
/// Form model for adding a position to a portfolio.
/// </summary>
public class AddPositionFormModel
{
    /// <summary>
    /// Gets or sets the portfolio ID.
    /// </summary>
    [Required(ErrorMessage = "Portfolio ID is required")]
    public int PortfolioId { get; set; }

    /// <summary>
    /// Gets or sets the ticker symbol.
    /// </summary>
    [Required(ErrorMessage = "Ticker is required")]
    [MinLength(1, ErrorMessage = "Ticker must be at least 1 character")]
    [MaxLength(10, ErrorMessage = "Ticker cannot exceed 10 characters")]
    public string Ticker { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quantity of shares.
    /// </summary>
    [Range(1, 1000000, ErrorMessage = "Quantity must be between 1 and 1,000,000")]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Gets or sets the entry price per share.
    /// </summary>
    [Range(0.01, 100000, ErrorMessage = "Entry price must be between $0.01 and $100,000")]
    public decimal EntryPrice { get; set; } = 1.0m;

    /// <summary>
    /// Gets or sets the entry date for the position.
    /// </summary>
    [Required(ErrorMessage = "Entry date is required")]
    public DateTime EntryDate { get; set; } = DateTime.Today;

    /// <summary>
    /// Gets or sets optional notes for the position.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }

    /// <summary>
    /// Normalizes the ticker symbol to uppercase.
    /// </summary>
    public void NormalizeTicker()
    {
        if (!string.IsNullOrWhiteSpace(Ticker))
        {
            Ticker = Ticker.ToUpperInvariant();
        }
    }
}
