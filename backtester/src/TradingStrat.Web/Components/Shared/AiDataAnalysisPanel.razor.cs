using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Shared;

public partial class AiDataAnalysisPanel : ComponentBase
{
    [Inject] private IDataAnalysisService AnalysisService { get; set; } = null!;

    [Parameter, EditorRequired]
    public required DataSummaryResult DataSummary { get; set; }

    private DataAnalysisResult? _analysis;
    private bool _isAnalyzing;

    private async Task AnalyzeAsync()
    {
        _isAnalyzing = true;
        StateHasChanged();

        try
        {
            _analysis = await AnalysisService.AnalyzeDataAsync(DataSummary);
        }
        finally
        {
            _isAnalyzing = false;
            StateHasChanged();
        }
    }

    private void ResetAnalysis()
    {
        _analysis = null;
    }

    private string GetSentimentClass() => _analysis?.Sentiment switch
    {
        "BULLISH" => "bg-green-100 dark:bg-green-900/20 border-2 border-green-500 dark:border-green-600 text-green-800 dark:text-green-300",
        "BEARISH" => "bg-red-100 dark:bg-red-900/20 border-2 border-red-500 dark:border-red-600 text-red-800 dark:text-red-300",
        _ => "bg-gray-100 dark:bg-gray-800 border-2 border-gray-500 text-gray-800 dark:text-gray-300"
    };

    private string GetConfidenceBarClass() => _analysis?.Confidence switch
    {
        >= 80 => "bg-green-600 dark:bg-green-500",
        >= 60 => "bg-yellow-600 dark:bg-yellow-500",
        >= 40 => "bg-orange-600 dark:bg-orange-500",
        _ => "bg-red-600 dark:bg-red-500"
    };
}
