using TradyStrat.Common.Time;
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TheAppManager.Modules;
using TradyStrat.Data;
using TradyStrat.Data.Sqlite;
using TradyStrat.Domain;

namespace TradyStrat.Modules;

public sealed class DatabaseModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        var dbPath = SqlitePathResolver.Expand(builder.Configuration["Database:Path"]!);
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        builder.Services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));

        builder.Services.AddScoped(typeof(IRepositoryBase<>),     typeof(EfRepositoryShim<>));
        builder.Services.AddScoped(typeof(IReadRepositoryBase<>), typeof(EfRepositoryShim<>));
        builder.Services.AddSingleton<IClock, SystemClock>();
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
    }
}

internal sealed class EfRepositoryShim<T>(AppDbContext db) : RepositoryBase<T>(db) where T : class { }
