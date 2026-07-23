namespace TradingViewBlazor.Components;

public class TechnicalAnalysis
    : TradingViewComponentBase<TechnicalAnalysisSettings>
{
    protected override string TradingViewScriptUrl
        => "https://s3.tradingview.com/external-embedding/embed-widget-technical-analysis.js";

    protected override string SerializeSettings(TechnicalAnalysisSettings settings)
        => JsonSerializer.Serialize(settings, SourceGenerationContext.Default.TechnicalAnalysisSettings);
}