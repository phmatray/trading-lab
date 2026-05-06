using TheAppManager.Modules;
using TradyStrat.Features.Shell;

namespace TradyStrat.Modules;

public sealed class HostingModule : IAppModule
{
    public void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(opt => opt.ListenLocalhost(5180));
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    }

    public void ConfigureMiddleware(WebApplication app)
    {
        app.UseStaticFiles();
        app.UseAntiforgery();
    }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    }
}
