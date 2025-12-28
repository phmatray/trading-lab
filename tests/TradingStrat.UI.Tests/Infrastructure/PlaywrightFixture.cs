namespace TradingStrat.UI.Tests.Infrastructure;

/// <summary>
/// Manages Playwright browser lifecycle for test execution.
/// Implements IAsyncLifetime for proper async setup and teardown.
/// </summary>
public class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    /// <summary>
    /// Gets the browser instance. Throws if not initialized.
    /// </summary>
    public IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Initializes Playwright and launches the browser based on TestConfiguration.
    /// </summary>
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();

        IBrowserType browserType = TestConfiguration.Browser.ToLowerInvariant() switch
        {
            "firefox" => _playwright.Firefox,
            "webkit" => _playwright.Webkit,
            _ => _playwright.Chromium
        };

        _browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = TestConfiguration.Headless,
            SlowMo = TestConfiguration.SlowMo,
            Timeout = 60_000 // 60 seconds for browser launch
        });
    }

    /// <summary>
    /// Closes the browser and disposes Playwright resources.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }
        _playwright?.Dispose();
    }

    /// <summary>
    /// Creates a new browser context with default settings for tests.
    /// Each test should use its own context for isolation.
    /// </summary>
    public async Task<IBrowserContext> CreateContextAsync()
    {
        return await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors = true, // For local development with self-signed certs
            AcceptDownloads = true,
            HasTouch = false,
            Locale = "en-US",
            TimezoneId = "America/New_York"
        });
    }
}
