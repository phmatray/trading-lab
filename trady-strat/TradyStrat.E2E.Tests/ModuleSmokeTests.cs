using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace TradyStrat.E2E.Tests.Modules;

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
        // Phase 2: GetAllTodaysSuggestionsUseCase is a Saga that swallows
        // per-ticker AI failures, so the dashboard can render in full even
        // when the dummy Anthropic key fails — there's no longer a guaranteed
        // "Could not load dashboard" branch on a fresh DB. Accept any of
        // (loading placeholder, error fallback, fully rendered dash-stage)
        // as proof the route resolved without a 500.
        (body.Contains("Loading")
            || body.Contains("Could not load dashboard")
            || body.Contains("dash-stage"))
            .ShouldBeTrue();
    }
}
