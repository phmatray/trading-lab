using TheAppManager.Startup;
using TradyStrat.Application;
using TradyStrat.Infrastructure;

AppManager.Start(args,
    modules => modules
        .AddFromAssemblyOf<ApplicationAssemblyMarker>()
        .AddFromAssemblyOf<InfrastructureAssemblyMarker>()
        .Add<TradyStrat.BlazorHostingModule>(),
    builder =>
    {
        // Web-host-specific configuration: v3 IAppModule can't reach WebApplicationBuilder.WebHost.
        builder.WebHost.ConfigureKestrel(opt => opt.ListenLocalhost(5180));
    });

namespace TradyStrat
{
    public partial class Program;
}
