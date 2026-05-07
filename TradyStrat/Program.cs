using TheAppManager.Startup;
using TradyStrat.Modules;

// Discover modules from the TradyStrat assembly explicitly so module discovery
// works under WebApplicationFactory<Program>, where Assembly.GetEntryAssembly()
// returns the test runner rather than this app.
AppManager.Start(args, modules => modules.AddFromAssemblyOf<DatabaseModule>());

namespace TradyStrat
{
    public partial class Program { }
}
