using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Spectre.Console.Cli;
using TheAppManager.Startup;
using TradyStrat.Application;
using TradyStrat.Cli;
using TradyStrat.Cli.Commands;
using TradyStrat.Infrastructure;
using TradyStrat.Infrastructure.PriceFeed; // for typeof(PriceFeedBackgroundInfrastructureModule)

// Pin ContentRootPath to AppContext.BaseDirectory so appsettings.json (copied
// next to the executable) loads regardless of the shell's working directory.
// Without this, launching the CLI from outside TradyStrat.Cli/ — e.g. from
// Claude Desktop, which spawns processes with $HOME as cwd — fails because
// Directory.GetCurrentDirectory() doesn't contain the config.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

// Load user-secrets unconditionally (generic hosts default to Production
// where user-secrets are normally skipped). The CLI is local-only by design
// and Anthropic:ApiKey is stored as a user-secret per the README.
builder.Configuration.AddUserSecrets<CliAssemblyMarker>(optional: true);

// Route all host-level logging to stderr so stdout stays clean for any
// commands that write structured output (e.g. the mcp command's JSON-RPC stream).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

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
    c.AddCommand<ReplayCommand>("replay")
     .WithDescription("Replay the AI prompt against historical snapshots and score the results.");
    c.AddCommand<McpCommand>("mcp")
     .WithDescription("Run the read-only TradyStrat MCP server over stdio.");
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
