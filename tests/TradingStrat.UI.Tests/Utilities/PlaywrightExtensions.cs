namespace TradingStrat.UI.Tests.Utilities;

/// <summary>
/// Extension methods for Playwright IPage and ILocator to simplify common test operations.
/// </summary>
public static class PlaywrightExtensions
{
    /// <summary>
    /// Fills an input field and triggers blur event to ensure Blazor validation fires.
    /// </summary>
    public static async Task FillAndBlurAsync(this ILocator locator, string value)
    {
        await locator.FillAsync(value);
        await locator.BlurAsync();
        await Task.Delay(100); // Small delay for validation to trigger
    }

    /// <summary>
    /// Asserts that the element is visible using Shouldly.
    /// </summary>
    public static async Task ShouldBeVisibleAsync(this ILocator locator, string? message = null)
    {
        bool isVisible = await locator.IsVisibleAsync();
        isVisible.ShouldBeTrue(message ?? $"Expected element to be visible: {locator}");
    }

    /// <summary>
    /// Asserts that the element is hidden using Shouldly.
    /// </summary>
    public static async Task ShouldBeHiddenAsync(this ILocator locator, string? message = null)
    {
        bool isHidden = await locator.IsHiddenAsync();
        isHidden.ShouldBeTrue(message ?? $"Expected element to be hidden: {locator}");
    }

    /// <summary>
    /// Asserts that the element is enabled using Shouldly.
    /// </summary>
    public static async Task ShouldBeEnabledAsync(this ILocator locator, string? message = null)
    {
        bool isEnabled = await locator.IsEnabledAsync();
        isEnabled.ShouldBeTrue(message ?? $"Expected element to be enabled: {locator}");
    }

    /// <summary>
    /// Asserts that the element is disabled using Shouldly.
    /// </summary>
    public static async Task ShouldBeDisabledAsync(this ILocator locator, string? message = null)
    {
        bool isDisabled = await locator.IsDisabledAsync();
        isDisabled.ShouldBeTrue(message ?? $"Expected element to be disabled: {locator}");
    }

    /// <summary>
    /// Asserts that the element contains specific text using Shouldly.
    /// </summary>
    public static async Task ShouldContainTextAsync(this ILocator locator, string expectedText, string? message = null)
    {
        string? actualText = await locator.TextContentAsync();
        actualText.ShouldNotBeNull();
        actualText.ShouldContain(expectedText);
    }

    /// <summary>
    /// Asserts that the element has exact text using Shouldly.
    /// </summary>
    public static async Task ShouldHaveTextAsync(this ILocator locator, string expectedText, string? message = null)
    {
        string? actualText = await locator.TextContentAsync();
        actualText?.Trim().ShouldBe(expectedText);
    }

    /// <summary>
    /// Clicks an element and waits for navigation to complete with Blazor SignalR connection.
    /// </summary>
    public static async Task ClickAndNavigateAsync(this ILocator locator, IPage page)
    {
        await locator.ClickAsync();
        await page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Takes a screenshot on test failure and saves it to the Screenshots directory.
    /// </summary>
    public static async Task TakeScreenshotOnFailureAsync(this IPage page, string testName)
    {
        string screenshotPath = TestConfiguration.ScreenshotPath;
        Directory.CreateDirectory(screenshotPath);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"{testName}_{timestamp}.png";
        string fullPath = Path.Combine(screenshotPath, fileName);

        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = fullPath,
            FullPage = true
        });
    }

    /// <summary>
    /// Waits for a specific number of elements matching the locator.
    /// </summary>
    public static async Task ShouldHaveCountAsync(this ILocator locator, int expectedCount, string? message = null)
    {
        int actualCount = await locator.CountAsync();
        actualCount.ShouldBe(expectedCount, message ?? $"Expected {expectedCount} elements but found {actualCount}");
    }

    /// <summary>
    /// Gets the value attribute of an input element.
    /// </summary>
    public static async Task<string> GetValueAsync(this ILocator locator)
    {
        return await locator.InputValueAsync();
    }

    /// <summary>
    /// Clears an input field by selecting all and deleting.
    /// </summary>
    public static async Task ClearAsync(this ILocator locator)
    {
        await locator.FillAsync(string.Empty);
    }

    /// <summary>
    /// Selects an option by value and waits for Blazor to process the change.
    /// </summary>
    public static async Task SelectAndBlurAsync(this ILocator locator, string value)
    {
        await locator.SelectOptionAsync(value);
        await locator.BlurAsync();
        await Task.Delay(100);
    }

    /// <summary>
    /// Checks a checkbox and waits for Blazor to process the change.
    /// </summary>
    public static async Task CheckAndBlurAsync(this ILocator locator)
    {
        await locator.CheckAsync();
        await locator.BlurAsync();
        await Task.Delay(100);
    }

    /// <summary>
    /// Unchecks a checkbox and waits for Blazor to process the change.
    /// </summary>
    public static async Task UncheckAndBlurAsync(this ILocator locator)
    {
        await locator.UncheckAsync();
        await locator.BlurAsync();
        await Task.Delay(100);
    }
}
