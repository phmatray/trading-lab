namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Live Analysis page (/analysis).
/// Represents the ML-based live analysis interface.
/// </summary>
public class LiveAnalysisPage : BasePage
{
    public LiveAnalysisPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/analysis";

    // Page Elements
    private ILocator PageTitle => Page.Locator("main h1");
    private ILocator TickerInput => Page.Locator("#ticker");
    private ILocator FetchFreshDataCheckbox => Page.Locator("input[type='checkbox']");
    private ILocator BuyThresholdInput => Page.Locator("#buy-threshold");
    private ILocator SellThresholdInput => Page.Locator("#sell-threshold");
    private ILocator SubmitButton => Page.Locator("button[type='submit']");
    private ILocator ProgressIndicator => Page.Locator("[data-testid='progress-indicator']").Or(Page.Locator("text=Analyzing"));
    private ILocator ErrorMessage => Page.Locator("[role='alert'] >> text=/Error/");
    private ILocator WarningMessage => Page.Locator("[role='alert'] >> text=/Warning/");

    // Results Elements
    private ILocator NoResultsPlaceholder => Page.Locator("text=No analysis results");
    private ILocator CurrentPrice => Page.Locator("text=Current Price").Locator("..");
    private ILocator PredictedSignal => Page.Locator("text=ML Prediction").Locator("..").Locator("span").First;
    private ILocator TechnicalIndicators => Page.Locator("text=Technical Indicators");

    // Technical Indicator Accordions
    private ILocator PriceBasedAccordion => Page.Locator("summary:has-text('Price-Based')");
    private ILocator MovingAveragesAccordion => Page.Locator("summary:has-text('Moving Averages')");
    private ILocator MomentumAccordion => Page.Locator("summary:has-text('Momentum')");
    private ILocator MACDAccordion => Page.Locator("summary:has-text('MACD')");
    private ILocator VolatilityAccordion => Page.Locator("summary:has-text('Volatility')");
    private ILocator VolumeAccordion => Page.Locator("summary:has-text('Volume')");

    /// <summary>
    /// Gets the main page title text.
    /// </summary>
    public async Task<string?> GetPageTitleAsync()
    {
        return await PageTitle.TextContentAsync();
    }

    /// <summary>
    /// Checks if the submit button is disabled.
    /// </summary>
    public async Task<bool> IsSubmitButtonDisabledAsync()
    {
        return await SubmitButton.IsDisabledAsync();
    }

    /// <summary>
    /// Fills the analysis form with the given values.
    /// </summary>
    public async Task FillAnalysisFormAsync(string ticker, bool fetchFreshData = false, decimal? buyThreshold = null, decimal? sellThreshold = null)
    {
        await TickerInput.FillAsync(ticker);

        if (fetchFreshData)
        {
            await FetchFreshDataCheckbox.CheckAsync();
        }

        if (buyThreshold.HasValue)
        {
            await BuyThresholdInput.FillAsync(buyThreshold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (sellThreshold.HasValue)
        {
            await SellThresholdInput.FillAsync(sellThreshold.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Submits the analysis form.
    /// </summary>
    public async Task SubmitFormAsync()
    {
        await SubmitButton.ClickAsync();
    }

    /// <summary>
    /// Waits for the analysis to complete.
    /// </summary>
    public async Task WaitForAnalysisCompleteAsync(int timeoutMs = 30000)
    {
        try
        {
            // Wait for progress indicator to appear
            await ProgressIndicator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 2000
            });
        }
        catch (TimeoutException)
        {
            // Progress might not appear for fast operations
        }

        try
        {
            // Wait for progress indicator to disappear
            await ProgressIndicator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = timeoutMs
            });
        }
        catch (TimeoutException)
        {
            // Already hidden or never appeared
        }

        await Task.Delay(500); // Additional delay for results rendering
    }

    /// <summary>
    /// Checks if results are displayed.
    /// </summary>
    public async Task<bool> AreResultsDisplayedAsync()
    {
        return await CurrentPrice.IsVisibleAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if the "no results" placeholder is displayed.
    /// </summary>
    public async Task<bool> IsNoResultsPlaceholderDisplayedAsync()
    {
        return await NoResultsPlaceholder.IsVisibleAsync();
    }

    /// <summary>
    /// Checks if an error message is displayed.
    /// </summary>
    public async Task<bool> HasErrorMessageAsync()
    {
        return await ErrorMessage.IsVisibleAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if a warning message is displayed.
    /// </summary>
    public async Task<bool> HasWarningMessageAsync()
    {
        return await WarningMessage.IsVisibleAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the predicted signal text (Buy/Sell/Hold).
    /// </summary>
    public async Task<string?> GetPredictedSignalAsync()
    {
        if (await AreResultsDisplayedAsync())
        {
            return await PredictedSignal.TextContentAsync();
        }
        return null;
    }

    /// <summary>
    /// Checks if technical indicators section is visible.
    /// </summary>
    public async Task<bool> AreTechnicalIndicatorsVisibleAsync()
    {
        return await TechnicalIndicators.IsVisibleAsync();
    }

    /// <summary>
    /// Expands a technical indicator accordion.
    /// </summary>
    public async Task ExpandIndicatorAccordionAsync(string accordionName)
    {
        ILocator accordion = accordionName.ToLower() switch
        {
            "price-based" => PriceBasedAccordion,
            "moving averages" => MovingAveragesAccordion,
            "momentum" => MomentumAccordion,
            "macd" => MACDAccordion,
            "volatility" => VolatilityAccordion,
            "volume" => VolumeAccordion,
            _ => throw new ArgumentException($"Unknown accordion: {accordionName}")
        };

        await accordion.ClickAsync();
        await Task.Delay(300); // Wait for expansion animation
    }
}
