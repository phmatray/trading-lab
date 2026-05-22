using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.AiSuggestion.Backfill;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.AiSuggestion.Snapshot.Sections;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Domain.Suggestions.Services;

namespace TradyStrat.Application.AiSuggestion;

public sealed class AiSuggestionApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<ICorrectnessRule>(_ => new FixedThresholdCorrectness(2.0m));
        services.AddScoped<IAiSnapshotService, AiSnapshotService>();
        services.AddScoped<ISnapshotSectionProvider, GoalSection>();
        services.AddScoped<ISnapshotSectionProvider, TickersSection>();
        services.AddScoped<ISnapshotSectionProvider, PortfolioSection>();
        services.AddScoped<ISnapshotSectionProvider, RecentTradesSection>();
        services.AddScoped<ISnapshotSectionProvider, MarketsSection>();
        services.AddScoped<ISnapshotSectionProvider, UsdPerEurSection>();
        services.AddScoped<ISnapshotSectionProvider, RecentSuggestionsSection>();

        services.AddScoped<ForwardReturnCalculator>();

        services.AddScoped<GetTodaysSuggestionUseCase>();
        services.AddScoped<GetAllTodaysSuggestionsUseCase>();
        services.AddScoped<StreamTodaysSuggestionsUseCase>();
        services.AddScoped<QuerySuggestionsUseCase>();
        services.AddScoped<BackfillSuggestionsUseCase>();
        services.AddScoped<ForceRefetchSuggestionUseCase>();
        services.AddScoped<ReplaySuggestionsUseCase>();

        services.AddSingleton<ISuggestionBackfillCoordinator, SuggestionBackfillCoordinator>();
    }
}
