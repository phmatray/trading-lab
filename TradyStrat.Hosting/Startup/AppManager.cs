using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheAppManager.Modules;

namespace TheAppManager.Startup;

public static class AppManager
{
    /// <summary>
    /// Host-neutral composition. Runs each module's ConfigureServices against the supplied
    /// service collection. Returns the composed module collection so callers can reuse the
    /// same module instances for middleware/endpoint passes.
    /// </summary>
    public static AppModuleCollection ConfigureServices(
        IServiceCollection services,
        IConfiguration config,
        Action<AppModuleCollection> configureModules)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(configureModules);

        var collection = new AppModuleCollection();
        configureModules(collection);

        foreach (var module in collection.GetModules())
            module.ConfigureServices(services, config);

        return collection;
    }

    public static void Start(
        string[] args,
        Action<AppModuleCollection> configureModules,
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        configureBuilder?.Invoke(builder);

        var modules = ConfigureServices(builder.Services, builder.Configuration, configureModules);

        // Legacy pass: v2-era modules implement ConfigureServices(WebApplicationBuilder).
        // Modules that only implement the host-neutral signature get the default no-op here.
        foreach (var module in modules.GetModules())
            module.ConfigureServices(builder);

        var app = builder.Build();

        foreach (var module in modules.GetModules())
            module.ConfigureMiddleware(app);

        foreach (var module in modules.GetModules())
            module.ConfigureEndpoints(app);

        app.Run();
    }

    public static async Task StartAsync(
        string[] args,
        Action<AppModuleCollection> configureModules,
        Action<WebApplicationBuilder>? configureBuilder = null)
    {
        var builder = WebApplication.CreateBuilder(args);
        configureBuilder?.Invoke(builder);

        var modules = ConfigureServices(builder.Services, builder.Configuration, configureModules);

        foreach (var module in modules.GetModules())
            module.ConfigureServices(builder);

        var app = builder.Build();

        foreach (var module in modules.GetModules())
            module.ConfigureMiddleware(app);

        foreach (var module in modules.GetModules())
            module.ConfigureEndpoints(app);

        await app.RunAsync();
    }
}
