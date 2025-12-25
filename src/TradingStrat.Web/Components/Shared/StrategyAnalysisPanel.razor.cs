using Microsoft.AspNetCore.Components;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Strategies;
using TradingStrat.Domain.Entities;
using TradingStrat.Web.Models;
using TradingStrat.Web.Services;

namespace TradingStrat.Web.Components.Shared;

public partial class StrategyAnalysisPanel : ComponentBase
{
    [Inject] private IAnalyzeStrategyUseCase AnalyzeStrategyUseCase { get; set; } = null!;
    [Inject] private IStrategyRegistry StrategyRegistry { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;

    [Parameter, EditorRequired]
    public required string Ticker { get; set; }

    [Parameter, EditorRequired]
    public required string StrategyType { get; set; }

    [Parameter]
    public Dictionary<string, object>? StrategyParameters { get; set; }

    private StrategyRecommendation? _recommendation;
    private bool _isAnalyzing;
    private string? _error;

    private async Task AnalyzeStrategyAsync()
    {
        _isAnalyzing = true;
        _error = null;
        StateHasChanged();

        try
        {
            // Parse string strategy type to enum
            var strategyTypeEnum = StrategyRegistry.ParseStrategyType(StrategyType);

            var command = new AnalyzeStrategyCommand(
                Ticker,
                strategyTypeEnum,
                StrategyParameters
            );
            _recommendation = await AnalyzeStrategyUseCase.ExecuteAsync(command);

            // Trigger recommendation notification
            await NotificationService.AddNotificationAsync(
                NotificationType.Recommendation,
                _recommendation.Confidence >= 70
                    ? NotificationSeverity.Info
                    : NotificationSeverity.Warning,
                "Strategy Recommendation",
                _recommendation.Summary,
                ticker: Ticker
            );
        }
        catch (Exception ex)
        {
            _error = $"Analysis failed: {ex.Message}";
        }
        finally
        {
            _isAnalyzing = false;
            StateHasChanged();
        }
    }

    private void ResetPanel()
    {
        _recommendation = null;
        _error = null;
        _isAnalyzing = false;
    }

    private string GetRecommendationCardClass()
    {
        if (_recommendation == null)
        {
            return "bg-gray-50 dark:bg-dark-elevated border border-gray-200 dark:border-dark-border";
        }

        return _recommendation.Confidence switch
        {
            >= 70 => "bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 text-green-900 dark:text-green-300",
            >= 40 => "bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 text-yellow-900 dark:text-yellow-300",
            _ => "bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-900 dark:text-red-300"
        };
    }

    private string GetConfidenceBarClass()
    {
        if (_recommendation == null)
        {
            return "bg-gray-400 dark:bg-gray-600";
        }

        return _recommendation.Confidence switch
        {
            >= 70 => "bg-green-600 dark:bg-green-500",
            >= 40 => "bg-yellow-600 dark:bg-yellow-500",
            _ => "bg-red-600 dark:bg-red-500"
        };
    }

    private string GetPriorityBadgeClass(string priority) => priority.ToLowerInvariant() switch
    {
        "high" => "bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-300 badge-high",
        "medium" => "bg-yellow-100 dark:bg-yellow-900/30 text-yellow-800 dark:text-yellow-300 badge-medium",
        "low" => "bg-green-100 dark:bg-green-900/30 text-green-800 dark:text-green-300 badge-low",
        _ => "bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300"
    };
}
