using Anthropic.SDK;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.AiSuggestion;
using TradyStrat.Infrastructure.Exceptions;

namespace TradyStrat.Infrastructure.AiSuggestion;

public sealed class AiSuggestionInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var apiKey = config["Anthropic:ApiKey"]
            ?? throw new AnthropicConfigurationException("Anthropic:ApiKey is not configured.");

        services.AddSingleton<IChatClient>(_ =>
            new AnthropicClient(apiKey)
                .Messages
                .AsBuilder()
                .UseFunctionInvocation()
                .Build());

        services.AddScoped<IAiClient, SuggestionService>();
    }
}
