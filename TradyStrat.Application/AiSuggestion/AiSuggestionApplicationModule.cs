using TradyStrat.Domain.Suggestions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.AiSuggestion.Snapshot;
using TradyStrat.Application.AiSuggestion.Snapshot.Sections;
using TradyStrat.Domain;

namespace TradyStrat.Application.AiSuggestion;

public sealed class AiSuggestionApplicationModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        // Phase 3 work-in-progress: use case registrations + RecentSuggestionsSection +
        // SuggestionBackfillCoordinator + ForwardReturnCalculator are .bak'd while the
        // Suggestion AR rewrite stabilizes. Restored once Tasks 14-18 land.
        services.AddSingleton<ICorrectnessRule>(_ => new FixedThresholdCorrectness(2.0m));
        services.AddScoped<IAiSnapshotService, AiSnapshotService>();
        services.AddScoped<ISnapshotSectionProvider, GoalSection>();
        services.AddScoped<ISnapshotSectionProvider, TickersSection>();
        services.AddScoped<ISnapshotSectionProvider, PortfolioSection>();
        services.AddScoped<ISnapshotSectionProvider, RecentTradesSection>();
        services.AddScoped<ISnapshotSectionProvider, MarketsSection>();
        services.AddScoped<ISnapshotSectionProvider, UsdPerEurSection>();
    }
}
