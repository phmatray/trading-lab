using TradingStrat.Models;

namespace TradingStrat.Services;

public interface IExportService
{
    Task ExportToCsvAsync(List<HistoricalPrice> data, string filePath);
    Task ExportToJsonAsync(List<HistoricalPrice> data, string filePath);
    Task ExportBacktestResultAsync(BacktestResult result, string filePath);
    Task ExportLiveAnalysisAsync(LiveAnalysisResult result, string filePath);
}
