using TradingStrat.Models;
using TradingStrat.Services.Strategies.MachineLearning;

namespace TradingStrat.Services.LiveAnalysis;

public interface ILiveAnalysisEngine
{
    Task<LiveAnalysisResult> AnalyzeCurrentPositionAsync(
        string ticker,
        PredictionThresholds? thresholds = null,
        IProgress<string>? progress = null);
}
