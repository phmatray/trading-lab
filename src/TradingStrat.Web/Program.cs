using Serilog;
using TradingStrat.Application.DependencyInjection;
using TradingStrat.Infrastructure.DependencyInjection;
using TradingStrat.Infrastructure.Persistence.EfCore;
using TradingStrat.Web.Components;
using TradingStrat.Web.DependencyInjection;

namespace TradingStrat.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog (reuse CLI pattern)
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Host.UseSerilog();

        // Add response compression for performance
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
        });

        // Add Blazor Server services
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Register TradingStrat layers (hexagonal architecture)
        builder.Services
            .AddApplication(builder.Configuration)
            .AddInfrastructure(builder.Configuration)
            .AddWeb();

        var app = builder.Build();

        // Ensure database exists (same as CLI)
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TradingContext>();
            await context.Database.EnsureCreatedAsync();
        }

        // Configure HTTP pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseResponseCompression();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

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
