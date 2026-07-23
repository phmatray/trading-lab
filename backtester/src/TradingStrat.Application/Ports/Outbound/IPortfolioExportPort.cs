using TradingStrat.Domain.Entities;

namespace TradingStrat.Application.Ports.Outbound;

/// <summary>
/// Outbound port for portfolio import/export operations.
/// Implemented by infrastructure layer (e.g., PortfolioCsvAdapter).
/// </summary>
public interface IPortfolioExportPort
{
    /// <summary>
    /// Exports portfolio positions to a CSV file.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID to export.</param>
    /// <param name="filePath">The file path to write the CSV.</param>
    Task ExportPortfolioToCsvAsync(int portfolioId, string filePath);

    /// <summary>
    /// Imports positions from a CSV file into a portfolio.
    /// </summary>
    /// <param name="portfolioId">The portfolio ID to import into.</param>
    /// <param name="filePath">The file path to read the CSV from.</param>
    /// <returns>List of imported positions.</returns>
    Task<List<Position>> ImportPositionsFromCsvAsync(int portfolioId, string filePath);
}
