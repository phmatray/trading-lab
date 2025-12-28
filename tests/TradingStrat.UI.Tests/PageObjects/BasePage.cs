namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Base class for all Page Object Model classes.
/// Provides common functionality for page navigation and element interaction.
/// </summary>
public abstract class BasePage
{
    protected readonly IPage Page;
    protected readonly string BaseUrl;

    protected BasePage(IPage page, string baseUrl)
    {
        Page = page;
        BaseUrl = baseUrl;
    }

    /// <summary>
    /// The relative path for this page (e.g., "/", "/backtest", "/data").
    /// </summary>
    protected abstract string PagePath { get; }

    /// <summary>
    /// Navigates to this page and waits for Blazor SignalR connection.
    /// </summary>
    public virtual async Task NavigateAsync()
    {
        await Page.GotoAsync($"{BaseUrl}{PagePath}");
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Gets the page title.
    /// </summary>
    public async Task<string> GetTitleAsync()
    {
        return await Page.TitleAsync();
    }

    /// <summary>
    /// Checks if the page is currently displayed by verifying the URL.
    /// </summary>
    public Task<bool> IsDisplayedAsync()
    {
        string currentUrl = Page.Url;
        return Task.FromResult(currentUrl.Contains(PagePath));
    }

    /// <summary>
    /// Waits for the page to finish loading any async operations.
    /// </summary>
    public async Task WaitForLoadAsync()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Takes a screenshot of the current page state.
    /// </summary>
    public async Task<byte[]> TakeScreenshotAsync()
    {
        return await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            FullPage = true
        });
    }

    /// <summary>
    /// Gets an alert message if present on the page.
    /// </summary>
    protected ILocator GetAlert()
    {
        return Page.Locator("[role='alert']")
            .Or(Page.Locator(".alert"))
            .Or(Page.Locator("[data-testid='alert-message']"));
    }

    /// <summary>
    /// Checks if an alert message is displayed.
    /// </summary>
    public async Task<bool> HasAlertAsync()
    {
        return await GetAlert().IsVisibleAsync();
    }

    /// <summary>
    /// Gets the text content of the alert message.
    /// </summary>
    public async Task<string?> GetAlertTextAsync()
    {
        if (await HasAlertAsync())
        {
            return await GetAlert().TextContentAsync();
        }
        return null;
    }

    /// <summary>
    /// Navigation menu locator (left sidebar with Catalyst Sidebar component).
    /// Targets the sidebar navigation specifically, not the top navbar.
    /// </summary>
    protected ILocator NavMenu => Page.Locator("aside[data-testid='left-sidebar'] nav");

    /// <summary>
    /// Navigates using the navigation menu in the left sidebar.
    /// </summary>
    protected async Task NavigateViaMenuAsync(string linkText)
    {
        await NavMenu.Locator($"text={linkText}").ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Gets the value from a metric card by locating the text-3xl paragraph element.
    /// This is a common pattern across metric cards in the application.
    /// </summary>
    /// <param name="cardLocator">The locator for the metric card container.</param>
    /// <param name="valueSelector">Optional custom selector for the value element. Defaults to "p.text-3xl".</param>
    /// <returns>The text content of the value element, or null if not found.</returns>
    protected async Task<string?> GetCardValueAsync(ILocator cardLocator, string valueSelector = "p.text-3xl")
    {
        ILocator valueElement = cardLocator.Locator(valueSelector);
        return await valueElement.TextContentAsync();
    }
}
