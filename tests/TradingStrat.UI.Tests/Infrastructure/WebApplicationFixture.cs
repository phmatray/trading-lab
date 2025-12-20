using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TradingStrat.Infrastructure.Persistence.EfCore;

namespace TradingStrat.UI.Tests.Infrastructure;

/// <summary>
/// Manages hosting of the Blazor Server application with a real Kestrel server for E2E tests.
/// Inherits from WebApplicationFactory to leverage built-in Blazor configuration,
/// but overrides CreateHost to use Kestrel instead of TestServer (which Playwright requires).
/// </summary>
public class WebApplicationFixture : WebApplicationFactory<TradingStrat.Web.Program>, IAsyncLifetime
{
    private IHost? _kestrelHost;
    private string? _baseAddress;

    /// <summary>
    /// Gets the base address of the hosted Kestrel application.
    /// </summary>
    public string BaseAddress
    {
        get
        {
            EnsureServer();
            return _baseAddress ?? throw new InvalidOperationException("Server not started");
        }
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Build the TestHost first (this sets up all services correctly)
        IHost testHost = builder.Build();

        // Configure a second host to use Kestrel on a dynamic port
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseKestrel();
            webHostBuilder.UseUrls("http://127.0.0.1:0");
        });

        // Build and start the Kestrel host
        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        // Extract the actual address from Kestrel
        IServer? server = _kestrelHost.Services.GetRequiredService<IServer>();
        IServerAddressesFeature? addresses = server.Features.Get<IServerAddressesFeature>();

        if (addresses != null && addresses.Addresses.Any())
        {
            _baseAddress = addresses.Addresses.First();
            ClientOptions.BaseAddress = new Uri(_baseAddress);
        }
        else
        {
            throw new InvalidOperationException("Could not determine Kestrel server address");
        }

        // Start the test host and return it
        testHost.Start();
        return testHost;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Load test-specific configuration
            config.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json"),
                optional: false,
                reloadOnChange: false);
        });

        builder.ConfigureServices(services =>
        {
            // Override dependencies for testing
            services.AddScoped<IReadOnlyList<Domain.Entities.HistoricalPrice>>(
                _ => new List<Domain.Entities.HistoricalPrice>());
        });
    }

    private void EnsureServer()
    {
        if (_kestrelHost == null)
        {
            // Trigger server creation by creating a client
            using HttpClient _ = CreateDefaultClient();
        }
    }

    public async Task InitializeAsync()
    {
        // Ensure database exists
        using IServiceScope scope = Services.CreateScope();
        TradingContext context = scope.ServiceProvider.GetRequiredService<TradingContext>();
        await context.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        // Stop and dispose the Kestrel host
        if (_kestrelHost != null)
        {
            await _kestrelHost.StopAsync();
            _kestrelHost.Dispose();
        }

        // Clean up test database
        string testDbPath = TestConfiguration.TestDatabasePath;
        if (File.Exists(testDbPath))
        {
            try
            {
                File.Delete(testDbPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        await base.DisposeAsync();
    }
}
