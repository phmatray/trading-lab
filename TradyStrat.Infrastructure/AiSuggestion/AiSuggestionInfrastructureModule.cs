using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Application.Settings;
using TradyStrat.Infrastructure.Exceptions;

namespace TradyStrat.Infrastructure.AiSuggestion;

public sealed class AiSuggestionInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var apiKey = config["Anthropic:ApiKey"]
            ?? throw new AnthropicConfigurationException("Anthropic:ApiKey is not configured.");

        // Decorator chain (spec §6.3):
        //   CacheControl  — translates SuggestionService's content-level cache-breakpoint
        //                   flag into the SDK-native CacheControl marker (Phase 5).
        //   Thinking      — applies WithThinking(ai.ThinkingBudget) to ChatOptions.
        //   Harvest       — pulls thinking text out of the response under a stable key.
        //   FunctionInvocation — Anthropic's tool-call dispatch (unchanged from pre-spec).
        // Scoped (not Singleton) because ThinkingChatClient needs Scoped IAnthropicSettingsRepository.
        // Cost of rebuilding the chain per scope is negligible compared to the network call.
        services.AddScoped<IChatClient>(sp =>
            new AnthropicClient(apiKey)
                .Messages
                .AsBuilder()
                .Use(inner => new CacheControlChatClient(inner))
                .Use(inner => new ThinkingChatClient(inner, sp.GetRequiredService<IAnthropicSettingsRepository>()))
                .Use(inner => new ThinkingHarvestChatClient(inner))
                .UseFunctionInvocation()
                .Build());

        services.AddScoped<ISuggestionRepository, EfSuggestionRepository>();
        services.AddScoped<IAiClient, SuggestionService>();
    }
}
