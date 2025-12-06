using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TradingStrat.Application.DependencyInjection;
using TradingStrat.Infrastructure.DependencyInjection;
using TradingStrat.Infrastructure.Persistence.EfCore;
using TradingStrat.Presentation.Console;
using TradingStrat.Presentation.DependencyInjection;

namespace TradingStrat.Presentation;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Configure configuration sources
        builder.Configuration
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Services.AddSerilog();

        // Register services
        builder.Services
            .AddApplication(builder.Configuration)
            .AddInfrastructure(builder.Configuration)
            .AddPresentation();

        var host = builder.Build();

        try
        {
            // Ensure database is created
            using (var scope = host.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<TradingContext>();
                await context.Database.EnsureCreatedAsync();
            }

            // Run the menu
            var menu = host.Services.GetRequiredService<ProgramMenu>();
            await menu.RunAsync();
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
