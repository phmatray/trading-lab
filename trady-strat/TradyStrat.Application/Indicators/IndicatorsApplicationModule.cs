using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.Indicators.Bollinger;
using TradyStrat.Application.Indicators.Ichimoku;
using TradyStrat.Application.Indicators.MovingAverage;
using TradyStrat.Application.Indicators.Rsi;
using TradyStrat.Application.Indicators.History;
using TradyStrat.Domain.Indicators.Services;

namespace TradyStrat.Application.Indicators;

public sealed class IndicatorsApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<IZoneRule, BollingerZoneRule>();
        services.AddSingleton<IZoneRule, RsiZoneRule>();
        services.AddSingleton<IZoneRule, MovingAverageZoneRule>();
        services.AddSingleton<IZoneRule, IchimokuZoneRule>();
        services.AddScoped<ZoneClassifier>();
        services.AddScoped<IIndicatorHistoryProvider, RsiHistoryProvider>();
        services.AddScoped<IIndicatorHistoryProvider, BollingerHistoryProvider>();
        services.AddScoped<IIndicatorHistoryProvider, IchimokuHistoryProvider>();
        services.AddScoped<IIndicatorHistoryProvider, Sma200HistoryProvider>();
        services.AddScoped<IIndicatorHistoryProviderFactory, IndicatorHistoryProviderFactory>();
        services.AddScoped<IndicatorEngine>();
        services.AddScoped<IIndicatorEngine>(sp => sp.GetRequiredService<IndicatorEngine>());
    }
}
