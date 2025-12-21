namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the AI Assistant Widget (floating chat).
/// Represents the AI chat interface that appears on various pages.
/// </summary>
public class AiAssistantWidgetPage : BasePage
{
    public AiAssistantWidgetPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/backtest"; // Widget appears on multiple pages

    // Widget Elements
    private ILocator MinimizedButton => Page.Locator("button:has-text('AI Assistant')");
    private ILocator ChatHeader => Page.Locator("h3:has-text('Trading Assistant')");
    private ILocator MinimizeButton => Page.Locator("button").Filter(new() { Has = Page.Locator("svg path[d*='M19 9l-7 7-7-7']") });
    private ILocator MessageInput => Page.Locator("input[placeholder*='Ask about strategies']");
    private ILocator SendButton => Page.Locator("button[type='button']").Filter(new() { Has = Page.Locator("svg path[d*='M12 19l9 2']") });
    private ILocator ClearHistoryButton => Page.Locator("button:has-text('Clear history')");
    private ILocator UserMessages => Page.Locator(".flex.justify-end");
    private ILocator AssistantMessages => Page.Locator(".flex.justify-start");

    /// <summary>
    /// Checks if the widget is minimized.
    /// </summary>
    public async Task<bool> IsMinimizedAsync()
    {
        return await MinimizedButton.IsVisibleAsync();
    }

    /// <summary>
    /// Checks if the widget is expanded.
    /// </summary>
    public async Task<bool> IsExpandedAsync()
    {
        return await ChatHeader.IsVisibleAsync();
    }

    /// <summary>
    /// Clicks the minimized button to expand the widget.
    /// </summary>
    public async Task ExpandAsync()
    {
        await MinimizedButton.ClickAsync();
        await Page.WaitForTimeoutAsync(300);
    }

    /// <summary>
    /// Clicks the minimize button to collapse the widget.
    /// </summary>
    public async Task MinimizeAsync()
    {
        await MinimizeButton.ClickAsync();
        await Page.WaitForTimeoutAsync(300);
    }

    /// <summary>
    /// Checks if the chat interface elements are visible.
    /// </summary>
    public async Task<bool> AreChatElementsVisibleAsync()
    {
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
    /// Checks if the dark theme is applied to the widget.
    /// </summary>
    public async Task<bool> IsDarkThemeAppliedAsync()
    {
        // Check if dark theme classes are applied
        ILocator minimizedBtn = MinimizedButton;
        string? className = await minimizedBtn.GetAttributeAsync("class");
        return className?.Contains("dark:") ?? false;
    }
}
