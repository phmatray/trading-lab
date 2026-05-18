using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.AiSuggestion.Backfill;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion;

public sealed class AiSuggestionApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ICorrectnessRule>(_ => new FixedThresholdCorrectness(2.0m));
        services.AddScoped<IAiSnapshotService, AiSnapshotService>();
        services.AddScoped<GetTodaysSuggestionUseCase>();
        services.AddScoped<GetAllTodaysSuggestionsUseCase>();
        services.AddScoped<ForceRefetchSuggestionUseCase>();
        services.AddScoped<BackfillSuggestionsUseCase>();
        services.AddSingleton<ISuggestionBackfillCoordinator>(sp =>
            new SuggestionBackfillCoordinator(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<SuggestionBackfillCoordinator>>()));
    }
}
