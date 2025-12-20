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
        // Load environment variables from .env file (for API keys)
        string envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
        if (File.Exists(envPath))
        {
            DotNetEnv.Env.Load(envPath);

            // Map ANTHROPIC_API_KEY from .env to Trading:Assistant:ApiKey config
            string? apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                Environment.SetEnvironmentVariable("Trading__Assistant__ApiKey", apiKey);
            }
        }

        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

        // Ensure database exists (same as CLI)
        using (IServiceScope scope = app.Services.CreateScope())
        {
            TradingContext context = scope.ServiceProvider.GetRequiredService<TradingContext>();
            await context.Database.EnsureCreatedAsync();
        }

        // Configure HTTP pipeline
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseResponseCompression();

        // Only redirect to HTTPS when HTTPS is actually configured
        // Docker runs HTTP-only, so skip HTTPS redirection in that scenario
        string urls = app.Configuration["ASPNETCORE_URLS"] ?? string.Empty;
        if (urls.Contains("https", StringComparison.OrdinalIgnoreCase))
        {
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        // Map SignalR hub for AI chat
        app.MapHub<TradingStrat.Web.Hubs.ChatHub>("/chathub");

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
