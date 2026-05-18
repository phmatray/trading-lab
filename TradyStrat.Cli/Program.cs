using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using TheAppManager.Startup;
using TradyStrat.Application;
using TradyStrat.Cli;
using TradyStrat.Cli.Commands;
using TradyStrat.Infrastructure;
using TradyStrat.Infrastructure.PriceFeed; // for typeof(PriceFeedBackgroundInfrastructureModule)

var builder = Host.CreateApplicationBuilder(args);

// Compose modules into the host's service collection. The CLI excludes
// PriceFeedBackgroundInfrastructureModule (background hosted service that
// polls Yahoo for prices) — a one-shot CLI command shouldn't start it.
AppManager.ConfigureServices(builder.Services, builder.Configuration, modules => modules
    .AddFromAssemblyOf<ApplicationAssemblyMarker>()
    .AddFromAssemblyOf<InfrastructureAssemblyMarker>(t =>
        t != typeof(PriceFeedBackgroundInfrastructureModule)));

// Construct Spectre registrar. It owns its own ServiceCollection for command
// types and falls back to the host's IServiceProvider for everything else.
var registrar = new HostTypeRegistrar();
var app = new CommandApp(registrar);
app.Configure(c =>
{
    c.AddCommand<HelloCommand>("hello").WithDescription("Verifies the CLI is wired.");
});

// Build the host. The registrar's two-provider design means Spectre's command
// registrations don't need to be in builder.Services.
using var host = builder.Build();
registrar.BindHost(host);

// Bracket Spectre.RunAsync with explicit IHost start/stop so any IHostedService
// gets a chance to initialize before the command runs (and dispose afterward).
await host.StartAsync();
try
{
    return await app.RunAsync(args);
}
finally
{
    await host.StopAsync();
}
