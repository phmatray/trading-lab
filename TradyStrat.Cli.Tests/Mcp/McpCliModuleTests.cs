using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;
using Shouldly;
using TheAppManager.Startup;
using TradyStrat.Application;
using TradyStrat.Cli;
using TradyStrat.Infrastructure;
using TradyStrat.Infrastructure.PriceFeed;
using Xunit;

namespace TradyStrat.Cli.Tests.Mcp;

public class McpCliModuleTests
{
    [Fact]
    public void All_six_tools_registered_and_di_graph_resolves()
    {
        var builder = Host.CreateApplicationBuilder();

        // Provide the minimum configuration required by infrastructure modules so the
        // module ConfigureServices calls succeed. No actual DB connection or API call
        // is made — EF opens connections lazily, and the Anthropic client is never invoked.
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Database:Path"] = Path.Combine(Path.GetTempPath(), "tradystrat-test.db"),
            ["Tickers:Focus"] = "CON3.L",
            // AiSuggestionInfrastructureModule throws at registration time if absent.
            ["Anthropic:ApiKey"] = "test-key-not-used-in-wiring-smoke",
        });

        AppManager.ConfigureServices(builder.Services, builder.Configuration, modules => modules
            .AddFromAssemblyOf<ApplicationAssemblyMarker>()
            .AddFromAssemblyOf<InfrastructureAssemblyMarker>(t =>
                t != typeof(PriceFeedBackgroundInfrastructureModule))
            .AddFromAssemblyOf<CliAssemblyMarker>());

        using var host = builder.Build();

        // The SDK registers one McpServerTool singleton per [McpServerTool]-decorated method.
        // Each of our six tool classes has exactly one decorated method → expect 6 tools.
        var tools = host.Services.GetServices<McpServerTool>().ToList();
        tools.Count.ShouldBe(6, $"Expected 6 MCP tools, got: {string.Join(", ", tools.Select(t => t.ProtocolTool.Name))}");

        // Verify all expected tool names are present.
        var names = tools.Select(t => t.ProtocolTool.Name).ToHashSet();
        names.ShouldContain("list_instruments");
        names.ShouldContain("get_dashboard");
        names.ShouldContain("query_suggestions");
        names.ShouldContain("query_prices");
        names.ShouldContain("get_portfolio");
        names.ShouldContain("get_replay_report");
    }
}
