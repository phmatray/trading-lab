using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
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
            // Clear default configuration sources and use only test configuration
            config.Sources.Clear();

            // Load base configuration first
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

            // Override with test-specific configuration
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
            // Trigger server creation by accessing Server property
            // This will call CreateHost() which sets up the Kestrel host
            _ = Server;
        }
    }

    public async Task InitializeAsync()
    {
        // Trigger server creation first (this will call Program.cs which creates the database)
        EnsureServer();

        // Now seed test data using the Kestrel host's services
        if (_kestrelHost != null)
        {
            using IServiceScope scope = _kestrelHost.Services.CreateScope();
            TradingContext context = scope.ServiceProvider.GetRequiredService<TradingContext>();

            // Seed test data for common tickers
            await SeedTestDataAsync(context);
        }
    }

    private static async Task SeedTestDataAsync(TradingContext context)
    {
        // Log database connection
        string? connectionString = context.Database.GetConnectionString();
        Console.WriteLine($"[Test DB Seeder] Database connection: {connectionString}");

        // Check if data already exists
        bool hasData = await context.HistoricalPrices.AnyAsync();
        Console.WriteLine($"[Test DB Seeder] Database has data: {hasData}");

        if (hasData)
        {
            Console.WriteLine("[Test DB Seeder] Skipping seed - data already exists");
            return;
        }

        Console.WriteLine("[Test DB Seeder] Starting data seed...");

        // Generate 500 days of synthetic historical data for test tickers
        string[] testTickers = { "MSFT", "AAPL", "GOOGL" };
        DateTime startDate = DateTime.Today.AddDays(-500);

        foreach (string ticker in testTickers)
        {
            decimal basePrice = ticker switch
            {
                "MSFT" => 300m,
                "AAPL" => 150m,
                "GOOGL" => 100m,
                _ => 100m
            };

            List<Domain.Entities.HistoricalPrice> prices = new();

            for (int i = 0; i < 500; i++)
            {
                DateTime date = startDate.AddDays(i);

                // Generate realistic OHLCV data with some volatility
                decimal randomFactor = 1m + ((decimal)(new Random(ticker.GetHashCode() + i).NextDouble() - 0.5) * 0.04m);
                decimal close = basePrice * randomFactor;
                decimal open = close * (1m + ((decimal)(new Random(i).NextDouble() - 0.5) * 0.02m));
                decimal high = Math.Max(open, close) * (1m + (decimal)new Random(i + 1).NextDouble() * 0.03m);
                decimal low = Math.Min(open, close) * (1m - (decimal)new Random(i + 2).NextDouble() * 0.03m);
                long volume = 1000000 + (long)(new Random(i + 3).NextDouble() * 5000000);

                prices.Add(new Domain.Entities.HistoricalPrice
                {
                    Ticker = ticker,
                    DateTime = date,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = volume,
                    ISIN = $"{ticker}_ISIN"
                });

                // Update base price for next day (trending)
                basePrice *= randomFactor;
            }

            Console.WriteLine($"[Test DB Seeder] Generated {prices.Count} prices for {ticker}");
            await context.HistoricalPrices.AddRangeAsync(prices);
        }

        await context.SaveChangesAsync();
        int totalCount = await context.HistoricalPrices.CountAsync();
        Console.WriteLine($"[Test DB Seeder] Seed complete! Total records in database: {totalCount}");
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
