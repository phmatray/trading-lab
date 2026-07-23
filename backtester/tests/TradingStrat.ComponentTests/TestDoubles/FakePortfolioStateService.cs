using TradingStrat.Web.Services.State;

namespace TradingStrat.ComponentTests.TestDoubles;

/// <summary>
/// Fake implementation of PortfolioStateService for testing.
/// Stores portfolio state in-memory instead of localStorage.
/// Each test gets a new instance, providing automatic isolation.
/// </summary>
public class FakePortfolioStateService : PortfolioStateService
{
    public FakePortfolioStateService() : base(new FakeLocalStorageService())
    {
    }
}
