using Microsoft.Extensions.AI;
using TheAppManager.Modules;
using TradyStrat.Features.AiSuggestion.UseCases;
using TradyStrat.Features.AiSuggestion;
using TradyStrat.Features.AiSuggestion.Backfill;
using TradyStrat.Features.AiSuggestion.Snapshot;
using TradyStrat.Common.Exceptions;

namespace TradyStrat.Modules;

public sealed class AiSuggestionModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var apiKey = builder.Configuration["Anthropic:ApiKey"]
            ?? throw new AnthropicConfigurationException("Anthropic:ApiKey is not configured.");
        var model = builder.Configuration["Anthropic:Model"] ?? "claude-opus-4-7";

        // MessagesEndpoint directly implements IChatClient in Anthropic.SDK 5.10.
        // No AsChatClient() adapter is needed — .AsBuilder() is the M.E.AI extension
        // on IChatClient itself. The model is set via ConfigureOptions.
        builder.Services.AddSingleton<IChatClient>(_ =>
            new Anthropic.SDK.AnthropicClient(apiKey)
                .Messages
                .AsBuilder()
                .ConfigureOptions(o => o.ModelId = model)
                .UseFunctionInvocation()
                .Build());

        builder.Services.AddScoped<IAiSnapshotService, AiSnapshotService>();
        builder.Services.AddScoped<IAiClient, SuggestionService>();
        builder.Services.AddScoped<GetTodaysSuggestionUseCase>();
        builder.Services.AddScoped<GetAllTodaysSuggestionsUseCase>();
        builder.Services.AddScoped<ForceRefetchSuggestionUseCase>();
        builder.Services.AddScoped<BackfillSuggestionsUseCase>();
        builder.Services.AddSingleton<ISuggestionBackfillCoordinator>(sp =>
            new SuggestionBackfillCoordinator(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<SuggestionBackfillCoordinator>>()));
    }
}
