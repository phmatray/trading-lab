namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Strategy Copilot (RightPanel chat interface).
/// Represents the AI chat interface in the tabbed RightPanel.
/// </summary>
public class AiAssistantWidgetPage : BasePage
{
    public AiAssistantWidgetPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/backtest"; // Panel appears on multiple pages

    // RightPanel Elements
    private ILocator RightPanel => Page.Locator("[data-testid='right-panel']");
    private ILocator StrategyCopilotTab => Page.Locator("[data-testid='strategy-copilot-tab']");
    private ILocator CollapseButton => Page.Locator("[aria-label*='Collapse panel'], [aria-label*='Expand panel']");
    private ILocator ChatModeButton => Page.Locator("button:has-text('Chat')");
    private ILocator ChatHeader => Page.Locator("h3:has-text('Strategy Copilot')");
    private ILocator MessageInput => Page.Locator("input[placeholder*='Ask about strategies, positions, or market data']");
    private ILocator SendButton => Page.Locator("button").Filter(new() { Has = Page.Locator("svg path[d*='M12 19l9 2']") });
    private ILocator ClearHistoryButton => Page.Locator("button:has-text('Clear history')");
    private ILocator UserMessages => Page.Locator(".flex.justify-end");
    private ILocator AssistantMessages => Page.Locator(".flex.justify-start");

    /// <summary>
    /// Checks if the panel is collapsed (showing only icon bar).
    /// </summary>
    public async Task<bool> IsMinimizedAsync()
    {
        // Check if panel has collapsed width class or chat header is not visible
        return !await ChatHeader.IsVisibleAsync();
    }

    /// <summary>
    /// Checks if the panel is expanded (showing content).
    /// </summary>
    public async Task<bool> IsExpandedAsync()
    {
        // Ensure Strategy Copilot tab is active and chat interface is visible
        return await ChatHeader.IsVisibleAsync() && await MessageInput.IsVisibleAsync();
    }

    /// <summary>
    /// Expands the panel and activates Strategy Copilot tab in Chat mode.
    /// </summary>
    public async Task ExpandAsync()
    {
        // Click Strategy Copilot tab to activate it and expand if collapsed
        await StrategyCopilotTab.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // Try to click Chat mode button if visible
        if (await ChatModeButton.IsVisibleAsync())
        {
            await ChatModeButton.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
        }
    }

    /// <summary>
    /// Collapses the panel to icon bar.
    /// </summary>
    public async Task MinimizeAsync()
    {
        // If panel is expanded, click collapse button
        if (await ChatHeader.IsVisibleAsync())
        {
            await CollapseButton.ClickAsync();
            await Page.WaitForTimeoutAsync(300);
        }
    }

    /// <summary>
    /// Checks if the chat interface elements are visible.
    /// </summary>
    public async Task<bool> AreChatElementsVisibleAsync()
    {
        // Ensure Strategy Copilot tab is active and Chat mode is active
        await ExpandAsync();

        // Check if all chat elements are visible
        bool headerVisible = await ChatHeader.IsVisibleAsync();
        bool inputVisible = await MessageInput.IsVisibleAsync();
        bool sendVisible = await SendButton.IsVisibleAsync();
        return headerVisible && inputVisible && sendVisible;
    }

    /// <summary>
    /// Types a message in the input field.
    /// </summary>
    public async Task TypeMessageAsync(string message)
    {
        await MessageInput.FillAsync(message);
    }

    /// <summary>
    /// Clicks the send button.
    /// </summary>
    public async Task SendMessageAsync()
    {
        await SendButton.ClickAsync();
        await Page.WaitForTimeoutAsync(500);
    }

    /// <summary>
    /// Types and sends a message.
    /// </summary>
    public async Task SendFullMessageAsync(string message)
    {
        await TypeMessageAsync(message);
        await SendMessageAsync();
    }

    /// <summary>
    /// Checks if the send button is enabled.
    /// </summary>
    public async Task<bool> IsSendButtonEnabledAsync()
    {
        bool isDisabled = await SendButton.IsDisabledAsync();
        return !isDisabled;
    }

    /// <summary>
    /// Gets the count of user messages in the chat.
    /// </summary>
    public async Task<int> GetUserMessageCountAsync()
    {
        return await UserMessages.CountAsync();
    }

    /// <summary>
    /// Gets the count of assistant messages in the chat.
    /// </summary>
    public async Task<int> GetAssistantMessageCountAsync()
    {
        return await AssistantMessages.CountAsync();
    }

    /// <summary>
    /// Clears the chat history.
    /// </summary>
    public async Task ClearHistoryAsync()
    {
        await ClearHistoryButton.ClickAsync();
        await Page.WaitForTimeoutAsync(300);
    }

    /// <summary>
    /// Simulates pressing Enter key in the message input.
    /// </summary>
    public async Task PressEnterAsync()
    {
        await MessageInput.PressAsync("Enter");
        await Page.WaitForTimeoutAsync(500);
    }

    /// <summary>
    /// Gets the current value of the message input.
    /// </summary>
    public async Task<string> GetMessageInputValueAsync()
    {
        return await MessageInput.InputValueAsync();
    }

    /// <summary>
    /// Checks if the dark theme is applied to the panel.
    /// </summary>
    public async Task<bool> IsDarkThemeAppliedAsync()
    {
        // Check if dark theme classes are applied to the panel
        string? className = await RightPanel.GetAttributeAsync("class");
        return className?.Contains("dark:") ?? false;
    }
}
