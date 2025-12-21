namespace TradingStrat.UI.Tests;

/// <summary>
/// Test configuration settings with environment variable overrides.
/// </summary>
public static class TestConfiguration
{
    /// <summary>
    /// Base URL for the application under test.
    /// Override with environment variable TEST_BASE_URL.
    /// </summary>
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://localhost:5001";

    /// <summary>
    /// Default timeout for Playwright operations (30 seconds).
    /// </summary>
    public static int DefaultTimeout => 30_000;

    /// <summary>
    /// Timeout for Blazor SignalR connection establishment (10 seconds).
    /// </summary>
    public static int BlazorConnectionTimeout => 10_000;

    /// <summary>
    /// Timeout for long-running operations like data fetching (60 seconds).
    /// </summary>
    public static int LongOperationTimeout => 60_000;

    /// <summary>
    /// Browser to use for tests. Options: "chromium", "firefox", "webkit".
    /// Override with environment variable TEST_BROWSER.
    /// </summary>
    public static string Browser =>
        Environment.GetEnvironmentVariable("TEST_BROWSER") ?? "chromium";

    /// <summary>
    /// Run tests in headless mode (no visible browser window).
    /// Override with environment variable TEST_HEADLESS=false for debugging.
    /// </summary>
    public static bool Headless =>
        Environment.GetEnvironmentVariable("TEST_HEADLESS") != "false";

    /// <summary>
    /// Slow down operations by N milliseconds for debugging.
    /// Override with environment variable TEST_SLOWMO.
    /// </summary>
    public static int SlowMo =>
        int.TryParse(Environment.GetEnvironmentVariable("TEST_SLOWMO"), out int slowMo)
            ? slowMo
            : 0;

    /// <summary>
    /// Screenshot directory path.
    /// </summary>
    public static string ScreenshotPath =>
        Path.Combine(AppContext.BaseDirectory, "Screenshots");

    /// <summary>
    /// Test database connection string (separate from production).
    /// </summary>
    public static string TestDatabasePath =>
        Path.Combine(AppContext.BaseDirectory, "test-trading.db");
}
