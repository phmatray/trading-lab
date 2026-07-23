namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Strategy Analysis Panel component.
/// Can be used on both Backtest and Comparison pages.
/// </summary>
public class StrategyAnalysisPanelPage : BasePage
{
    public StrategyAnalysisPanelPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/backtest"; // Used on Backtest page by default

    // Locators
    private ILocator AnalyzeButton => Page.Locator("[data-testid='analyze-button']");

    private ILocator LoadingIndicator => Page.Locator("text=Analyzing strategy with AI");

    private ILocator ErrorMessage => Page.Locator(".bg-red-50.border-red-200");

    private ILocator SummaryCard => Page.Locator("h4:has-text('Summary')");

    private ILocator RecommendationCard => Page.Locator("h4:has-text('Recommendation')");

    private ILocator ConfidenceScore => Page.Locator("[data-testid='confidence-score']");

    private ILocator ActionItemsDetails => Page.Locator("details:has-text('Action Items')");

    private ILocator ActionItems => Page.Locator("details li");

    private ILocator HighPriorityBadges => Page.Locator(".badge-high");

    private ILocator NewAnalysisButton => Page.Locator("button:has-text('New Analysis')");

    /// <summary>
    /// Clicks the "Get AI Strategy Analysis" button.
    /// </summary>
    public async Task ClickAnalyzeButtonAsync()
    {
        await AnalyzeButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Checks if the analyze button is visible.
    /// </summary>
    public async Task<bool> IsAnalyzeButtonVisibleAsync()
    {
        try
        {
            return await AnalyzeButton.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if the loading indicator is visible.
    /// </summary>
    public async Task<bool> IsLoadingAsync()
    {
        try
        {
            return await LoadingIndicator.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for the AI analysis to complete (recommendation appears).
    /// </summary>
    public async Task WaitForAnalysisCompleteAsync(int timeout = 60000)
    {
        try
        {
            await RecommendationCard.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = timeout
            });
            await Task.Delay(500); // Additional delay for rendering
        }
        catch (TimeoutException ex)
        {
            throw new TimeoutException($"Analysis did not complete within {timeout}ms", ex);
        }
    }

    /// <summary>
    /// Checks if the recommendation is displayed.
    /// </summary>
    public async Task<bool> IsRecommendationDisplayedAsync()
    {
        try
        {
            return await RecommendationCard.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the confidence score text (e.g., "75%").
    /// </summary>
    public async Task<string> GetConfidenceScoreAsync()
    {
        try
        {
            string? text = await ConfidenceScore.TextContentAsync();
            return text?.Trim() ?? "0";
        }
        catch
        {
            return "0";
        }
    }

    /// <summary>
    /// Gets the count of action items.
    /// Expands the details element first if needed.
    /// </summary>
    public async Task<int> GetActionItemsCountAsync()
    {
        try
        {
            // Check if details is already open
            string? isOpen = await ActionItemsDetails.GetAttributeAsync("open");
            if (isOpen is null)
            {
                // Click to expand if closed
                await ActionItemsDetails.ClickAsync();
                await Task.Delay(200);
            }

            return await ActionItems.CountAsync();
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Checks if there are any high-priority action items.
    /// </summary>
    public async Task<bool> HasHighPriorityItemsAsync()
    {
        try
        {
            // Check if details is already open
            string? isOpen = await ActionItemsDetails.GetAttributeAsync("open");
            if (isOpen is null)
            {
                // Click to expand if closed
                await ActionItemsDetails.ClickAsync();
                await Task.Delay(200);
            }

            int count = await HighPriorityBadges.CountAsync();
            return count > 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Checks if an error message is displayed.
    /// </summary>
    public async Task<bool> HasErrorAsync()
    {
        try
        {
            return await ErrorMessage.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the error message text.
    /// </summary>
    public async Task<string> GetErrorMessageAsync()
    {
        try
        {
            string? text = await ErrorMessage.TextContentAsync();
            return text?.Trim() ?? "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Checks if the summary card is visible.
    /// </summary>
    public async Task<bool> IsSummaryCardVisibleAsync()
    {
        try
        {
            return await SummaryCard.IsVisibleAsync();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the summary card text.
    /// </summary>
    public async Task<string> GetSummaryTextAsync()
    {
        try
        {
            string? text = await SummaryCard.TextContentAsync();
            return text?.Trim() ?? "";
        }
        catch
        {
            return "";
        }
    }

    /// <summary>
    /// Clicks the "New Analysis" button to reset the panel.
    /// </summary>
    public async Task ClickNewAnalysisButtonAsync()
    {
        await NewAnalysisButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Counts how many analysis panels are visible on the current page.
    /// Useful for verifying dual panels on Comparison page.
    /// </summary>
    public async Task<int> GetPanelCountAsync()
    {
        ILocator panels = Page.Locator("[data-testid='analyze-button']");
        return await panels.CountAsync();
    }
}
