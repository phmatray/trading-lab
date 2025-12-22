namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Rebalancing page (/portfolio/{id}/rebalance).
/// Represents the portfolio rebalancing calculator interface.
/// </summary>
public class RebalancingPage : BasePage
{
    private readonly int _portfolioId;

    public RebalancingPage(IPage page, string baseUrl, int portfolioId) : base(page, baseUrl)
    {
        _portfolioId = portfolioId;
    }

    protected override string PagePath => $"/portfolio/{_portfolioId}/rebalance";

    // Page Elements
    private ILocator PageTitle => Page.Locator("main h1");
    private ILocator BackButton => Page.Locator("button[aria-label='Go back']");

    // Target Allocations Section
    private ILocator AddAllocationButton => Page.Locator("button:has-text('+ Add Position')");
    private ILocator AllocationInputs => Page.Locator(".grid.grid-cols-12");

    // Commission Settings
    private ILocator CommissionPercentageInput => Page.Locator("input[type='number']").Filter(new LocatorFilterOptions
    {
        HasText = "%"
    });
    private ILocator MinimumCommissionInput => Page.Locator("input[type='number']").Filter(new LocatorFilterOptions
    {
        HasText = "Minimum"
    });

    // Cash Percentage
    private ILocator CashPercentageInput => Page.Locator("input[type='range']");

    // Calculate Button
    private ILocator CalculateButton => Page.Locator("button:has-text('Calculate Rebalancing Plan')");

    // Rebalancing Plan Results
    private ILocator ResultsSection => Page.Locator(".bg-gray-800.rounded-lg");
    private ILocator SignalsTable => Page.Locator("table");
    private ILocator SignalRows => SignalsTable.Locator("tbody tr");
    private ILocator ExecutableStatus => Page.Locator(".badge");

    // Loading Indicator
    private ILocator LoadingSpinner => Page.Locator(".animate-spin");

    /// <summary>
    /// Gets the page title text.
    /// </summary>
    public async Task<string?> GetPageTitleAsync()
    {
        return await PageTitle.TextContentAsync();
    }

    /// <summary>
    /// Clicks the Add Allocation button.
    /// </summary>
    public async Task ClickAddAllocationAsync()
    {
        await AddAllocationButton.ClickAsync();
        await Task.Delay(200);
    }

    /// <summary>
    /// Adds a target allocation (ticker and percentage).
    /// </summary>
    public async Task AddTargetAllocationAsync(string ticker, decimal percentage)
    {
        await ClickAddAllocationAsync();

        // Get the last allocation input row (the one we just added)
        int allocationCount = await AllocationInputs.CountAsync();
        ILocator lastAllocation = AllocationInputs.Nth(allocationCount - 1);

        // Fill ticker input (first input in the row)
        ILocator tickerInput = lastAllocation.Locator("input").First;
        await tickerInput.FillAsync(ticker);

        // Fill percentage input (second input in the row)
        ILocator percentageInput = lastAllocation.Locator("input").Nth(1);
        await percentageInput.FillAsync(percentage.ToString());

        await Task.Delay(200);
    }

    /// <summary>
    /// Sets the commission percentage.
    /// </summary>
    public async Task SetCommissionPercentageAsync(decimal percentage)
    {
        await CommissionPercentageInput.FillAsync(percentage.ToString());
    }

    /// <summary>
    /// Sets the minimum commission amount.
    /// </summary>
    public async Task SetMinimumCommissionAsync(decimal amount)
    {
        await MinimumCommissionInput.FillAsync(amount.ToString());
    }

    /// <summary>
    /// Sets the cash percentage using the slider.
    /// </summary>
    public async Task SetCashPercentageAsync(decimal percentage)
    {
        // Range inputs in Playwright require string values
        await CashPercentageInput.FillAsync(percentage.ToString());
    }

    /// <summary>
    /// Clicks the Calculate Rebalancing Plan button.
    /// </summary>
    public async Task ClickCalculateAsync()
    {
        await CalculateButton.ClickAsync();
        await Task.Delay(500); // Wait for calculation
    }

    /// <summary>
    /// Waits for the loading spinner to disappear.
    /// </summary>
    public async Task WaitForCalculationToCompleteAsync(int timeoutMs = 10000)
    {
        try
        {
            await LoadingSpinner.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = timeoutMs
            });
        }
        catch
        {
            // Ignore timeout if spinner is not found or already hidden
        }
    }

    /// <summary>
    /// Checks if the rebalancing plan results section is visible.
    /// </summary>
    public async Task<bool> AreResultsVisibleAsync()
    {
        return await ResultsSection.IsVisibleAsync();
    }

    /// <summary>
    /// Gets the count of rebalancing signals in the table.
    /// </summary>
    public async Task<int> GetSignalCountAsync()
    {
        return await SignalRows.CountAsync();
    }

    /// <summary>
    /// Gets all signal actions (Buy, Sell, Hold) from the table.
    /// </summary>
    public async Task<List<string?>> GetSignalActionsAsync()
    {
        int count = await GetSignalCountAsync();
        List<string?> actions = new();

        for (int i = 0; i < count; i++)
        {
            ILocator row = SignalRows.Nth(i);
            ILocator actionCell = row.Locator("td").Nth(1); // Action is second column
            string? action = await actionCell.TextContentAsync();
            actions.Add(action?.Trim());
        }

        return actions;
    }

    /// <summary>
    /// Gets the executable status text.
    /// </summary>
    public async Task<string?> GetExecutableStatusAsync()
    {
        return await ExecutableStatus.TextContentAsync();
    }

    /// <summary>
    /// Checks if the rebalancing plan is executable.
    /// </summary>
    public async Task<bool> IsRebalancingExecutableAsync()
    {
        string? status = await GetExecutableStatusAsync();
        return status?.Contains("Executable") ?? false;
    }

    /// <summary>
    /// Gets a signal row by ticker symbol.
    /// </summary>
    public ILocator GetSignalRowByTicker(string ticker)
    {
        return SignalsTable.Locator($"tr:has-text('{ticker}')");
    }

    /// <summary>
    /// Checks if a specific ticker appears in the rebalancing signals.
    /// </summary>
    public async Task<bool> HasSignalForTickerAsync(string ticker)
    {
        ILocator row = GetSignalRowByTicker(ticker);
        return await row.IsVisibleAsync();
    }

    /// <summary>
    /// Gets the action (Buy, Sell, Hold) for a specific ticker.
    /// </summary>
    public async Task<string?> GetActionForTickerAsync(string ticker)
    {
        ILocator row = GetSignalRowByTicker(ticker);
        ILocator actionCell = row.Locator("td").Nth(1);
        return await actionCell.TextContentAsync();
    }

    /// <summary>
    /// Clicks the Back to Dashboard button.
    /// </summary>
    public async Task ClickBackToDashboardAsync()
    {
        await BackButton.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Configures a complete rebalancing scenario and calculates.
    /// </summary>
    public async Task ConfigureAndCalculateRebalancingAsync(
        Dictionary<string, decimal> targetAllocations,
        decimal cashPercentage = 0m,
        decimal commissionPercentage = 0.1m,
        decimal minimumCommission = 1.0m)
    {
        // Add target allocations
        foreach (KeyValuePair<string, decimal> allocation in targetAllocations)
        {
            await AddTargetAllocationAsync(allocation.Key, allocation.Value);
        }

        // Set cash percentage
        await SetCashPercentageAsync(cashPercentage);

        // Set commission settings
        await SetCommissionPercentageAsync(commissionPercentage);
        await SetMinimumCommissionAsync(minimumCommission);

        // Calculate
        await ClickCalculateAsync();
        await WaitForCalculationToCompleteAsync();
    }
}
