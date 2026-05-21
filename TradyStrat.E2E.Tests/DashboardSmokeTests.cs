using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace TradyStrat.E2E.Tests;

/// <summary>
/// Web factory tuned for dashboard rendering smoke tests: isolated SQLite file
/// + a dummy Anthropic key so the Saga can fail gracefully per-ticker without
/// blowing up the host. Mirrors the configuration pattern from
/// <c>ModuleSmokeTests</c>.
/// </summary>
public sealed class DashboardWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(
        Path.GetTempPath(),
        $"tradystrat-dashboard-smoke-{Guid.NewGuid()}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Database:Path", _dbPath);
        builder.UseSetting("Anthropic:ApiKey", "sk-ant-test-dummy-key-for-smoke-test");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); }
            catch { /* best-effort cleanup */ }
        }
    }
}

public class DashboardSmokeTests : IClassFixture<DashboardWebApplicationFactory>
{
    private readonly DashboardWebApplicationFactory _factory;

    public DashboardSmokeTests(DashboardWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Live_initial_render_includes_skeleton_marker()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Skeleton class is on the call card in Pending state.
        html.ShouldContain("call-card--skeleton");
    }

    [Fact]
    public async Task Historical_initial_render_has_no_skeleton()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/?on=2026-01-15", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        html.ShouldNotContain("call-card--skeleton");
    }
}
