using Ardalis.Specification;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;
using TradyStrat.Domain;
using TradyStrat.Domain.SeedWork;
using TradyStrat.Infrastructure.Data.Sqlite;
using TradyStrat.Infrastructure.SeedWork;
using TradyStrat.Infrastructure.Time;

namespace TradyStrat.Infrastructure.Data;

public sealed class DatabaseInfrastructureModule : IAppModule
{
    public void ConfigureServices(IServiceCollection services, IConfiguration config)
    {
        var dbPath = SqlitePathResolver.Expand(config["Database:Path"]!);
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        services.AddScoped(typeof(IRepositoryBase<>),     typeof(EfRepositoryShim<>));
        services.AddScoped(typeof(IReadRepositoryBase<>), typeof(EfRepositoryShim<>));
        services.AddSingleton<IClock, SystemClock>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
}
