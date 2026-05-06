using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace TradyStrat.Tests.Modules;

public class ModuleSmokeTests
{
    [Fact]
    public async Task Application_boots_and_responds_on_root_route()
    {
        var dbPath = Path.Combine(Path.GetTempPath(),
            $"tradystrat-smoke-{Guid.NewGuid()}.db");

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b =>
            {
                b.UseSetting("Database:Path", dbPath);
                b.UseSetting("Anthropic:ApiKey", "sk-ant-test-dummy-key-for-smoke-test");
            });

        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        var resp = await client.GetAsync("/", ct);

        resp.IsSuccessStatusCode.ShouldBeTrue();
        var body = await resp.Content.ReadAsStringAsync(ct);
        // Smoke DB is empty, so LoadDashboardUseCase fails fast with
        // IndicatorComputationException; page renders the error state.
        // Either branch (loading or error) is fine — both prove the page
        // routed and rendered without a 500.
        (body.Contains("Loading") || body.Contains("Could not load dashboard"))
            .ShouldBeTrue();
    }
}
