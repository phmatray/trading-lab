using TheAppManager.Modules;
using TradyStrat.Features.Indicators;

namespace TradyStrat.Modules;

public sealed class IndicatorsModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IZoneRule, BollingerZoneRule>();
        builder.Services.AddSingleton<IZoneRule, RsiZoneRule>();
        builder.Services.AddSingleton<IZoneRule, MovingAverageZoneRule>();
        builder.Services.AddSingleton<IZoneRule, IchimokuZoneRule>();
        builder.Services.AddScoped<ZoneClassifier>();
        builder.Services.AddScoped<IIndicatorHistoryProvider, RsiHistoryProvider>();
        builder.Services.AddScoped<IIndicatorHistoryProvider, BollingerHistoryProvider>();
        builder.Services.AddScoped<IIndicatorHistoryProvider, IchimokuHistoryProvider>();
        builder.Services.AddScoped<IIndicatorHistoryProvider, Sma200HistoryProvider>();
        builder.Services.AddScoped<IIndicatorHistoryProviderFactory, IndicatorHistoryProviderFactory>();
        builder.Services.AddScoped<IndicatorEngine>();
    }
}
