namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Strategy Copilot (RightPanel chat interface).
/// Tests the chat interface, message sending, and panel behavior.
/// </summary>
public class AiAssistantWidgetTests : BaseTest
{
    public AiAssistantWidgetTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task WhenLoaded_ShouldShowRightPanel()
    {
        // Arrange
        var widgetPage = new AiAssistantWidgetPage(Page!, BaseUrl);

        // Act
        await widgetPage.NavigateAsync();
        bool isExpanded = await widgetPage.IsExpandedAsync();

        // Assert
        isExpanded.ShouldBeTrue("RightPanel with Strategy Copilot should be visible by default");
    }

    [Fact]
    public async Task CollapseAndExpand_ShouldTogglePanel()
    {
        // Arrange
        var widgetPage = new AiAssistantWidgetPage(Page!, BaseUrl);
        await widgetPage.NavigateAsync();

        // Act - Collapse the panel
        await widgetPage.MinimizeAsync();
        bool isMinimized = await widgetPage.IsMinimizedAsync();

        // Then expand it again
        await widgetPage.ExpandAsync();
        bool isExpanded = await widgetPage.IsExpandedAsync();

        // Assert
        isMinimized.ShouldBeTrue("Panel should be collapsed after minimize");
        isExpanded.ShouldBeTrue("Panel should expand when Strategy Copilot tab is clicked");
    }

    [Fact]
    public async Task ExpandedPanel_ShouldShowChatInterface()
    {
        // Arrange
        var widgetPage = new AiAssistantWidgetPage(Page!, BaseUrl);
        await widgetPage.NavigateAsync();

        // Act
        await widgetPage.ExpandAsync();
        bool hasChatElements = await widgetPage.AreChatElementsVisibleAsync();

        // Assert
        hasChatElements.ShouldBeTrue("Expanded panel should show header, input, and send button");
    }

    [Fact]
    public async Task TypeMessage_ShouldEnableSubmit()
    {
        // Arrange
        var widgetPage = new AiAssistantWidgetPage(Page!, BaseUrl);
        await widgetPage.NavigateAsync();
        await widgetPage.ExpandAsync();

        // Act - Initially should be disabled
        bool initiallyEnabled = await widgetPage.IsSendButtonEnabledAsync();

        // Type a message
        await widgetPage.TypeMessageAsync("What is RSI strategy?");
        bool enabledAfterTyping = await widgetPage.IsSendButtonEnabledAsync();

        // Assert
        initiallyEnabled.ShouldBeFalse("Send button should be disabled when input is empty");
        enabledAfterTyping.ShouldBeTrue("Send button should be enabled after typing message");
    }

    [Fact]
    public async Task SendMessage_ShouldAppearInHistory()
    {
        // Arrange
        var widgetPage = new AiAssistantWidgetPage(Page!, BaseUrl);
        await widgetPage.NavigateAsync();
        await widgetPage.ExpandAsync();

        // Act
        int initialCount = await widgetPage.GetUserMessageCountAsync();
        await widgetPage.SendFullMessageAsync("Test message");
        await Task.Delay(1000); // Wait for message to render
        int afterCount = await widgetPage.GetUserMessageCountAsync();

        // Assert
        initialCount.ShouldBe(0, "Should have no messages initially");
        afterCount.ShouldBeGreaterThan(initialCount, "User message should appear in chat history");
    }

    [Fact]
    public async Task ClearHistory_ShouldRemoveAllMessages()
    {
        // Arrange
        var widgetPage = new AiAssistantWidgetPage(Page!, BaseUrl);
        await widgetPage.NavigateAsync();
        await widgetPage.ExpandAsync();
        await widgetPage.SendFullMessageAsync("Message 1");
        await Task.Delay(500);
        await widgetPage.SendFullMessageAsync("Message 2");
        await Task.Delay(500);

        // Act
        int beforeClear = await widgetPage.GetUserMessageCountAsync();
        await widgetPage.ClearHistoryAsync();
        await Task.Delay(300);
        int afterClear = await widgetPage.GetUserMessageCountAsync();

        // Assert
        beforeClear.ShouldBeGreaterThan(0, "Should have messages before clearing");
        afterClear.ShouldBe(0, "All messages should be removed after clearing history");
    }

    [Fact]
    public async Task Minimize_ShouldHidePanel()
    {
        // Arrange
        var widgetPage = new AiAssistantWidgetPage(Page!, BaseUrl);
        await widgetPage.NavigateAsync();
        await widgetPage.ExpandAsync();

        // Act
        bool expandedBefore = await widgetPage.IsExpandedAsync();
        await widgetPage.MinimizeAsync();
        bool minimizedAfter = await widgetPage.IsMinimizedAsync();

        // Assert
        expandedBefore.ShouldBeTrue("Should be expanded before minimizing");
        minimizedAfter.ShouldBeTrue("Should be minimized after clicking minimize button");
    }

    [Fact]
    public async Task EnterKey_ShouldSendMessage()
    {
        // Arrange
        var widgetPage = new AiAssistantWidgetPage(Page!, BaseUrl);
        await widgetPage.NavigateAsync();
        await widgetPage.ExpandAsync();

        // Act
        await widgetPage.TypeMessageAsync("Enter key test");
        int beforeCount = await widgetPage.GetUserMessageCountAsync();
        await widgetPage.PressEnterAsync();
        await Task.Delay(1000);
        int afterCount = await widgetPage.GetUserMessageCountAsync();

        // Assert
        afterCount.ShouldBeGreaterThan(beforeCount, "Message should be sent when Enter key is pressed");
    }

    [Fact]
    public async Task EmptyInput_ShouldDisableSubmit()
    {
        // Arrange
        var widgetPage = new AiAssistantWidgetPage(Page!, BaseUrl);
        await widgetPage.NavigateAsync();
        await widgetPage.ExpandAsync();

        // Act
        await widgetPage.TypeMessageAsync("Test");
        bool enabledWithText = await widgetPage.IsSendButtonEnabledAsync();

        await widgetPage.TypeMessageAsync(""); // Clear the input
        bool disabledWhenEmpty = !await widgetPage.IsSendButtonEnabledAsync();

        // Assert
        enabledWithText.ShouldBeTrue("Button should be enabled with text");
        disabledWhenEmpty.ShouldBeTrue("Button should be disabled when input is empty");
    }

    [Fact]
    public async Task DarkTheme_ShouldApply()
    {
        // Arrange
        var widgetPage = new AiAssistantWidgetPage(Page!, BaseUrl);
        await widgetPage.NavigateAsync();

        // Act
        bool hasDarkTheme = await widgetPage.IsDarkThemeAppliedAsync();

        // Assert - This is a visual verification
        // Dark theme classes should be present in the widget elements
        hasDarkTheme.ShouldBeTrue("Widget should have dark theme classes applied");
    }
}
