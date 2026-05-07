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
        builder.Services.AddScoped<IndicatorEngine>();
    }
}
