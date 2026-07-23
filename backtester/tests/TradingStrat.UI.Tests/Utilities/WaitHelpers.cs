namespace TradingStrat.UI.Tests.Utilities;

/// <summary>
/// Provides Blazor Server-specific wait helpers for handling SignalR connections and async operations.
/// </summary>
public static class WaitHelpers
{
    /// <summary>
    /// Waits for the Blazor SignalR connection to be established.
    /// This is critical for Blazor Server apps as components won't render until the connection is ready.
    /// </summary>
    public static async Task WaitForBlazorAsync(this IPage page)
    {
        // Wait for network to be idle
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Wait for Blazor to be defined on the window object
        // This indicates the SignalR connection is established
        await page.WaitForFunctionAsync(
            "() => window.Blazor !== undefined",
            new PageWaitForFunctionOptions
            {
                Timeout = TestConfiguration.BlazorConnectionTimeout
            });

        // Wait for Blazor to be fully stable (no reconnection modal)
        try
        {
            await page.WaitForFunctionAsync(
                "() => !document.querySelector('#components-reconnect-modal')",
                new PageWaitForFunctionOptions
                {
                    Timeout = 2000
                });
        }
        catch (TimeoutException)
        {
            // Reconnect modal doesn't exist (good), or already gone
        }

        // Small delay to ensure components are initialized
        await Task.Delay(100);
    }

    /// <summary>
    /// Waits for a progress indicator to appear and then disappear.
    /// Useful for operations like data fetching, backtesting, or ML predictions.
    /// </summary>
    public static async Task WaitForProgressIndicatorAsync(this IPage page)
    {
        // Look for common progress indicator patterns
        ILocator progressIndicator = page.Locator("[data-testid='progress-indicator']")
            .Or(page.Locator("text=Processing"))
            .Or(page.Locator("text=Loading"))
            .Or(page.Locator(".animate-spin"))
            .Or(page.Locator("text=Fetching data"))
            .Or(page.Locator("text=Running backtest"))
            .Or(page.Locator("text=Training model"));

        try
        {
            // Wait for progress indicator to appear (with short timeout)
            await progressIndicator.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 2000
            });

            // Wait for it to disappear (with long timeout for long operations)
            await progressIndicator.First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Hidden,
                Timeout = TestConfiguration.LongOperationTimeout
            });
        }
        catch (TimeoutException)
        {
            // Progress indicator might not appear for fast operations
            // This is acceptable, so we catch and ignore
        }

        // Small delay to ensure UI has updated
        await Task.Delay(200);
    }

    /// <summary>
    /// Waits for ApexCharts or TradingView charts to be rendered.
    /// Charts are rendered asynchronously after data is loaded.
    /// </summary>
    public static async Task WaitForChartAsync(this IPage page)
    {
        // Wait for ApexCharts canvas
        ILocator apexChart = page.Locator(".apexcharts-canvas");
        ILocator tradingViewChart = page.Locator(".tradingview-widget-container iframe");

        try
        {
            await apexChart.Or(tradingViewChart).First.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 10_000
            });

            // Additional delay for chart animations
            await Task.Delay(500);
        }
        catch (TimeoutException)
        {
            // Chart might not be present on all pages
            // This is acceptable in some contexts
        }
    }

    /// <summary>
    /// Waits for an alert message to appear (success, error, warning, or info).
    /// </summary>
    public static async Task WaitForAlertAsync(this IPage page, string? expectedText = null)
    {
        ILocator alert = page.Locator("[role='alert']")
            .Or(page.Locator(".alert"))
            .Or(page.Locator("[data-testid='alert-message']"));

        if (expectedText is not null)
        {
            alert = page.Locator($"text={expectedText}");
        }

        await alert.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 5000
        });
    }

    /// <summary>
    /// Waits for a form validation message to appear.
    /// </summary>
    public static async Task WaitForValidationMessageAsync(this IPage page, string? expectedMessage = null)
    {
        ILocator validationMessage = page.Locator(".validation-message")
            .Or(page.Locator("[role='alert']"))
            .Or(page.Locator(".text-red-600"));

        if (expectedMessage is not null)
        {
            validationMessage = page.Locator($"text={expectedMessage}");
        }

        await validationMessage.First.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible,
            Timeout = 3000
        });
    }

    /// <summary>
    /// Waits for Monaco editor to be fully initialized and ready for interaction.
    /// Checks for: Monaco object exists, helper is available, and editor DOM is rendered.
    /// </summary>
    public static async Task WaitForMonacoAsync(this IPage page, int timeoutMs = 15000)
    {
        await page.WaitForFunctionAsync(@"
            () => {
                // Check Monaco editor DOM element exists
                const monacoEditor = document.querySelector('.monaco-editor');
                if (!monacoEditor) return false;

                // Check Monaco global object is loaded
                if (typeof monaco === 'undefined') return false;

                // Check our helper is available
                if (!window.monacoEditorHelper) return false;

                // Check editor has rendered content (lines-content is the actual code view)
                const hasContent = monacoEditor.querySelector('.lines-content') !== null;
                return hasContent;
            }
        ", new PageWaitForFunctionOptions { Timeout = timeoutMs });

        // Small additional delay for stability
        await Task.Delay(500);
    }
}
