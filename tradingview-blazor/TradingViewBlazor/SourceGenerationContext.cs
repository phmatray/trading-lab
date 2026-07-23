namespace TradingViewBlazor;

[JsonSerializable(typeof(AdvancedRealTimeChartSettings))]
[JsonSerializable(typeof(CryptoCoinsHeatmapSettings))]
[JsonSerializable(typeof(FundamentalDataSettings))]
[JsonSerializable(typeof(MiniChartSettings))]
[JsonSerializable(typeof(StockHeatmapSettings))]
[JsonSerializable(typeof(TechnicalAnalysisSettings))]
[JsonSerializable(typeof(TickerSettings))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
internal partial class SourceGenerationContext
    : JsonSerializerContext;