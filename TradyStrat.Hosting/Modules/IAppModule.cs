using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TheAppManager.Modules;

public interface IAppModule
{
    /// <summary>Host-neutral service registration. Preferred for new modules.</summary>
    void ConfigureServices(IServiceCollection services, IConfiguration config) { }

    /// <summary>
    /// Legacy web-only registration retained for backward compatibility during the
    /// hexagonal-refactor transition. Existing v2-era modules implement this; new
    /// modules should prefer the host-neutral overload above. This method is removed
    /// once all consumers migrate (see Phase 5 in the refactor plan).
    /// </summary>
    void ConfigureServices(WebApplicationBuilder builder) { }

    void ConfigureMiddleware(WebApplication app) { }
    void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
}
