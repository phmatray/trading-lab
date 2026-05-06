using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using TradyStrat.Components;
using TradyStrat.Data;
using TradyStrat.Shared.Time;
using Xunit;

namespace TradyStrat.Tests.Modules;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> for smoke-testing.
/// <para>
/// <see cref="AppManager"/> auto-discovers modules via <c>Assembly.GetEntryAssembly()</c>,
/// which returns the test runner assembly (not <c>TradyStrat.dll</c>) when executed from a
/// test host.  We therefore override <see cref="CreateHost"/> to wire the
/// <see cref="WebApplication"/> directly with the same services and middleware that the
/// production modules provide, backed by an in-process <see cref="TestServer"/>.
/// </para>
/// </summary>
internal sealed class TradyStratSmokeFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath;

    public TradyStratSmokeFactory(string dbPath)
    {
        _dbPath = dbPath;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var appBuilder = WebApplication.CreateBuilder();
        appBuilder.WebHost.UseTestServer();

        // DatabaseModule — services
        var dbDir = Path.GetDirectoryName(_dbPath);
        if (!string.IsNullOrEmpty(dbDir))
            Directory.CreateDirectory(dbDir);

        appBuilder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite("Data Source=" + _dbPath));
        appBuilder.Services.AddScoped(
            typeof(IRepositoryBase<>), typeof(SmokeEfRepositoryShim<>));
        appBuilder.Services.AddScoped(
            typeof(IReadRepositoryBase<>), typeof(SmokeEfRepositoryShim<>));
        appBuilder.Services.AddSingleton<IClock, SystemClock>();

        // HostingModule — services
        appBuilder.Services.AddRazorComponents().AddInteractiveServerComponents();

        var app = appBuilder.Build();

        // DatabaseModule — middleware (run migrations)
        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>()
                .Database.Migrate();
        }

        // HostingModule — middleware and endpoints
        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        app.Start();
        return app.Services.GetRequiredService<IHost>();
    }
}

internal sealed class SmokeEfRepositoryShim<T>(AppDbContext db)
    : RepositoryBase<T>(db) where T : class { }

public class ModuleSmokeTests
{
    [Fact]
    public async Task Application_boots_and_responds_on_root_route()
    {
        var dbPath = Path.Combine(Path.GetTempPath(),
            $"tradystrat-smoke-{Guid.NewGuid()}.db");

        await using var factory = new TradyStratSmokeFactory(dbPath);
        using var client = factory.Server.CreateClient();

        var ct = TestContext.Current.CancellationToken;
        var resp = await client.GetAsync("/", ct);

        resp.IsSuccessStatusCode.ShouldBeTrue();
        var body = await resp.Content.ReadAsStringAsync(ct);
        body.ShouldContain("TradyStrat — bootstrap");
    }
}
