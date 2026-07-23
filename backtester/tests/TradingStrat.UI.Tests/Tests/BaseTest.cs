namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// Base class for all E2E tests.
/// Provides browser context and page lifecycle management.
/// </summary>
[Collection("Playwright")]
public abstract class BaseTest : IAsyncLifetime
{
    protected PlaywrightFixture PlaywrightFixture { get; }
    protected WebApplicationFixture AppFixture { get; }
    protected IBrowserContext? Context { get; private set; }
    protected IPage? Page { get; private set; }
    protected string BaseUrl => AppFixture.BaseAddress;

    protected BaseTest(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
    {
        PlaywrightFixture = playwrightFixture;
        AppFixture = appFixture;
    }

    /// <summary>
    /// Initializes a new browser context and page for each test.
    /// This ensures test isolation.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        Context = await PlaywrightFixture.CreateContextAsync();
        Page = await Context.NewPageAsync();
        Page.SetDefaultTimeout(TestConfiguration.DefaultTimeout);

        // Enable console logging for debugging
        Page.Console += (_, msg) =>
        {
            if (msg.Type == "error")
            {
                Console.WriteLine($"[Browser Console Error] {msg.Text}");
            }
        };

        // Log page errors
        Page.PageError += (_, exception) =>
        {
            Console.WriteLine($"[Page Error] {exception}");
        };
    }

    /// <summary>
    /// Cleans up the page and context after each test.
    /// Takes a screenshot on failure if configured.
    /// </summary>
    public virtual async Task DisposeAsync()
    {
        // Note: Screenshot on failure is typically handled by test framework hooks
        // But we can add manual screenshot capture here if needed

        if (Page is not null)
        {
            await Page.CloseAsync();
        }

        if (Context is not null)
        {
            await Context.CloseAsync();
        }
    }

    /// <summary>
    /// Helper method to navigate to a specific page and wait for load.
    /// </summary>
    protected async Task NavigateToAsync(string path)
    {
        if (Page is null)
        {
            throw new InvalidOperationException("Page is not initialized");
        }

        await Page.GotoAsync($"{BaseUrl}{path}");
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Helper method to take a screenshot with a descriptive name.
    /// </summary>
    protected async Task TakeScreenshotAsync(string name)
    {
        if (Page is null)
        {
            return;
        }

        string screenshotPath = TestConfiguration.ScreenshotPath;
        Directory.CreateDirectory(screenshotPath);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string testName = GetType().Name;
        string fileName = $"{testName}_{name}_{timestamp}.png";
        string fullPath = Path.Combine(screenshotPath, fileName);

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = fullPath,
            FullPage = true
        });

        Console.WriteLine($"Screenshot saved: {fullPath}");
    }
}
