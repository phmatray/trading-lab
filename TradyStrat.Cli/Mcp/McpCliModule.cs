using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Application.AiSuggestion.UseCases;
using TradyStrat.Application.Dashboard;
using TradyStrat.Application.Dashboard.UseCases;
using TradyStrat.Application.PriceFeed.UseCases;
using TradyStrat.Application.UseCases;
using TradyStrat.Cli.Mcp.Filters;
using TradyStrat.Cli.Mcp.Serialization;
using TradyStrat.Cli.Mcp.Tools;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp;

internal sealed class McpCliModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<Guards>();

        // The application modules register the concrete use-case types but the
        // tools inject via IUseCase<TIn,TOut>. Add the interface→concrete
        // forwarding registrations here so DI resolves them without modifying
        // the feature modules.
        services.AddScoped<IUseCase<LoadDashboardInput, DashboardViewModel>>(
            sp => sp.GetRequiredService<LoadDashboardUseCase>());

        services.AddScoped<IUseCase<QuerySuggestionsInput, QuerySuggestionsOutput>>(
            sp => sp.GetRequiredService<QuerySuggestionsUseCase>());

        services.AddScoped<IUseCase<GetPriceSeriesInput, GetPriceSeriesOutput>>(
            sp => sp.GetRequiredService<GetPriceSeriesUseCase>());

        services.AddScoped<IUseCase<ReplaySuggestionsInput, ReplayReport>>(
            sp => sp.GetRequiredService<ReplaySuggestionsUseCase>());

        var jsonOptions = McpJsonSerializerOptions.Create();

        services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithRequestFilters(rb => rb
                .AddCallToolFilter(McpLoggingFilter.AsFilter())
                .AddCallToolFilter(McpTimeoutFilter.AsFilter())
                .AddCallToolFilter(McpExceptionTranslationFilter.AsFilter()))
            .WithTools<InstrumentTool>(serializerOptions: jsonOptions)
            .WithTools<DashboardTool>(serializerOptions: jsonOptions)
            .WithTools<SuggestionTool>(serializerOptions: jsonOptions)
            .WithTools<PriceTool>(serializerOptions: jsonOptions)
            .WithTools<PortfolioTool>(serializerOptions: jsonOptions)
            .WithTools<ReplayTool>(serializerOptions: jsonOptions);
    }
}
