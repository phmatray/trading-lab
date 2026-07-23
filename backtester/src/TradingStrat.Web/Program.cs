using Microsoft.EntityFrameworkCore;
using Serilog;
using TradingStrat.Application.DependencyInjection;
using TradingStrat.Infrastructure.DependencyInjection;
using TradingStrat.Infrastructure.Persistence.EfCore;
using TradingStrat.Infrastructure.Python;
using TradingStrat.Web.Components;
using TradingStrat.Web.DependencyInjection;

namespace TradingStrat.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // Configure Serilog (reuse CLI pattern)
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog();

        // Add Blazor Server services
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Add SignalR for real-time chat streaming
        builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 102400; // 100 KB
        });

        // Register TradingStrat layers (hexagonal architecture)
        builder.Services
            .AddApplication(builder.Configuration)
            .AddInfrastructure(builder.Configuration)
            .AddWeb();

        WebApplication app = builder.Build();

        // Apply database migrations (ensure all tables exist including new ones like Portfolios)
        // Skip migrations in test environment (tests use EnsureCreated instead)
        string? connectionString = builder.Configuration.GetValue<string>("Trading:Database:ConnectionString");
        bool isTestDatabase = connectionString?.Contains("test-trading.db") ?? false;

        if (!isTestDatabase)
        {
            using IServiceScope scope = app.Services.CreateScope();
            TradingContext context = scope.ServiceProvider.GetRequiredService<TradingContext>();
            await context.Database.MigrateAsync();
        }

        // Initialize Python runtime for custom strategies
        try
        {
            using IServiceScope scope = app.Services.CreateScope();
            PythonEnvironmentManager pythonManager = scope.ServiceProvider.GetRequiredService<PythonEnvironmentManager>();
            pythonManager.Initialize();
            Log.Information("Python environment initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to initialize Python environment. Python strategies will not be available.");
        }

        // Configure HTTP pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Map SignalR hub for AI chat
        app.MapHub<Hubs.ChatHub>("/chathub");

        // Health check endpoint for Docker/Kubernetes
        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

        try
        {
            Log.Information("Starting TradingStrat Web Application");
            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
