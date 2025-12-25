using TradingStrat.Web.Services.State;

namespace TradingStrat.ComponentTests.TestDoubles;

/// <summary>
/// Fake implementation of ChatStateService for testing.
/// Stores chat history in-memory instead of localStorage.
/// Each test gets a new instance, providing automatic isolation.
/// </summary>
public class FakeChatStateService : ChatStateService
{
    public FakeChatStateService() : base(new FakeLocalStorageService())
    {
    }
}
