namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// xUnit collection definition for sharing fixtures across test classes.
/// All tests marked with [Collection("Playwright")] will share the same
/// PlaywrightFixture and WebApplicationFixture instances.
/// </summary>
[CollectionDefinition("Playwright")]
public class PlaywrightCollection :
    ICollectionFixture<PlaywrightFixture>,
    ICollectionFixture<WebApplicationFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
