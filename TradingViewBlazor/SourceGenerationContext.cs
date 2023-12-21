using System.Text.Json.Serialization;
using TradingViewBlazor.Components;

namespace TradingViewBlazor;

[JsonSerializable(typeof(AdvancedRealTimeChartSettings))]
[JsonSerializable(typeof(FundamentalDataSettings))]
[JsonSerializable(typeof(MiniChartSettings))]
[JsonSerializable(typeof(TechnicalAnalysisSettings))]
[JsonSerializable(typeof(TickerSettings))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseUpper)]
internal partial class SourceGenerationContext
    : JsonSerializerContext;