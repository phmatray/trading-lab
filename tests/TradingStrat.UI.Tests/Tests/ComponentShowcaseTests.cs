namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for Phase 2 UI components (Checkbox, Radio, Switch, Dialog).
/// Tests interactive behavior, state management, and accessibility.
/// </summary>
public class ComponentShowcaseTests : BaseTest
{
    public ComponentShowcaseTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    #region Checkbox Tests

    [Fact]
    public async Task CheckboxSection_WhenLoaded_ShouldBeVisible()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsCheckboxSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Checkbox section should be visible");
    }

    [Fact]
    public async Task Checkbox_WhenClicked_ShouldToggleState()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Initially unchecked
        bool initialState = await page.IsCheckboxCheckedAsync("accept-terms-checkbox");
        initialState.ShouldBeFalse("Checkbox should start unchecked");

        // Click to check
        await page.ClickAcceptTermsCheckboxAsync();
        await Page!.WaitForBlazorAsync();

        // Assert - Now checked
        bool afterClick = await page.IsCheckboxCheckedAsync("accept-terms-checkbox");
        afterClick.ShouldBeTrue("Checkbox should be checked after click");

        // Click to uncheck
        await page.ClickAcceptTermsCheckboxAsync();
        await Page!.WaitForBlazorAsync();

        // Assert - Unchecked again
        bool afterSecondClick = await page.IsCheckboxCheckedAsync("accept-terms-checkbox");
        afterSecondClick.ShouldBeFalse("Checkbox should be unchecked after second click");
    }

    [Fact]
    public async Task CheckboxGroup_WhenMultipleSelected_ShouldUpdateStatus()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Check notifications and auto-save
        await page.ClickNotificationsCheckboxAsync();
        await Page!.WaitForBlazorAsync();
        await page.ClickAutoSaveCheckboxAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        bool notifications = await page.IsCheckboxCheckedAsync("enable-notifications");
        bool darkMode = await page.IsCheckboxCheckedAsync("enable-darkmode");
        bool autoSave = await page.IsCheckboxCheckedAsync("enable-autosave");

        notifications.ShouldBeTrue("Notifications should be checked");
        darkMode.ShouldBeTrue("Dark mode should remain checked (default)");
        autoSave.ShouldBeTrue("Auto-save should be checked");

        string statusText = await page.GetFeaturesStatusTextAsync();
        statusText.ShouldContain("Notifications: True");
        statusText.ShouldContain("Dark Mode: True");
        statusText.ShouldContain("Auto-save: True");
    }

    [Fact]
    public async Task ColoredCheckbox_WhenClicked_ShouldToggle()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickBlueCheckboxAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        bool isChecked = await page.IsCheckboxCheckedAsync("checkbox-blue");
        isChecked.ShouldBeTrue("Blue checkbox should be checked");
    }

    [Fact]
    public async Task DisabledCheckbox_ShouldBeDisabled()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Assert
        bool isDisabled = await page.IsCheckboxDisabledAsync("disabled-checkbox");
        isDisabled.ShouldBeTrue("Disabled checkbox should be disabled");

        bool isChecked = await page.IsCheckboxCheckedAsync("disabled-checkbox");
        isChecked.ShouldBeTrue("Disabled checkbox should be checked");
    }

    #endregion

    #region Radio Tests

    [Fact]
    public async Task RadioSection_WhenLoaded_ShouldBeVisible()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsRadioSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Radio section should be visible");
    }

    [Fact]
    public async Task RadioGroup_WhenClicked_ShouldSelectOneOption()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Initially card should be selected
        bool cardInitial = await page.IsRadioCheckedAsync("radio-card");
        cardInitial.ShouldBeTrue("Card radio should start selected");

        // Act - Select PayPal
        await page.ClickPayPalRadioAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        bool cardAfter = await page.IsRadioCheckedAsync("radio-card");
        bool paypalAfter = await page.IsRadioCheckedAsync("radio-paypal");
        bool bankAfter = await page.IsRadioCheckedAsync("radio-bank");

        cardAfter.ShouldBeFalse("Card radio should be deselected");
        paypalAfter.ShouldBeTrue("PayPal radio should be selected");
        bankAfter.ShouldBeFalse("Bank radio should remain deselected");

        string statusText = await page.GetPaymentStatusTextAsync();
        statusText.ShouldContain("PayPal");
    }

    [Fact]
    public async Task RadioGroup_WhenSwitchingSelections_ShouldDeselectPrevious()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Switch from Card to Bank
        await page.ClickBankRadioAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        bool bank = await page.IsRadioCheckedAsync("radio-bank");
        bool card = await page.IsRadioCheckedAsync("radio-card");

        bank.ShouldBeTrue("Bank radio should be selected");
        card.ShouldBeFalse("Card radio should be deselected");
    }

    [Fact]
    public async Task ColoredRadio_WhenClicked_ShouldSelect()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Click high priority (red)
        await page.ClickPriorityHighRadioAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        bool high = await page.IsRadioCheckedAsync("priority-high");
        bool medium = await page.IsRadioCheckedAsync("priority-medium");
        bool low = await page.IsRadioCheckedAsync("priority-low");

        high.ShouldBeTrue("High priority should be selected");
        medium.ShouldBeFalse("Medium priority should be deselected");
        low.ShouldBeFalse("Low priority should remain deselected");
    }

    #endregion

    #region Switch Tests

    [Fact]
    public async Task SwitchSection_WhenLoaded_ShouldBeVisible()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsSwitchSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Switch section should be visible");
    }

    [Fact]
    public async Task Switch_WhenClicked_ShouldToggle()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act - Initially off
        bool initialState = await page.IsSwitchCheckedAsync("feature-switch");
        initialState.ShouldBeFalse("Switch should start unchecked");

        // Toggle on
        await page.ClickFeatureSwitchAsync();
        await Page!.WaitForBlazorAsync();

        // Assert - Now on
        bool afterClick = await page.IsSwitchCheckedAsync("feature-switch");
        afterClick.ShouldBeTrue("Switch should be checked after click");

        // Toggle off
        await page.ClickFeatureSwitchAsync();
        await Page!.WaitForBlazorAsync();

        // Assert - Off again
        bool afterSecondClick = await page.IsSwitchCheckedAsync("feature-switch");
        afterSecondClick.ShouldBeFalse("Switch should be unchecked after second click");
    }

    [Fact]
    public async Task SwitchGroup_WhenMultipleToggled_ShouldUpdateStatus()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Initial state: Analytics and Push are on, Marketing is off
        bool analyticsInitial = await page.IsSwitchCheckedAsync("analytics-switch");
        bool marketingInitial = await page.IsSwitchCheckedAsync("marketing-switch");
        bool pushInitial = await page.IsSwitchCheckedAsync("push-switch");

        analyticsInitial.ShouldBeTrue("Analytics should start enabled");
        marketingInitial.ShouldBeFalse("Marketing should start disabled");
        pushInitial.ShouldBeTrue("Push should start enabled");

        // Act - Toggle marketing on, analytics off
        await page.ClickMarketingSwitchAsync();
        await Page!.WaitForBlazorAsync();
        await page.ClickAnalyticsSwitchAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        bool analyticsAfter = await page.IsSwitchCheckedAsync("analytics-switch");
        bool marketingAfter = await page.IsSwitchCheckedAsync("marketing-switch");
        bool pushAfter = await page.IsSwitchCheckedAsync("push-switch");

        analyticsAfter.ShouldBeFalse("Analytics should be disabled");
        marketingAfter.ShouldBeTrue("Marketing should be enabled");
        pushAfter.ShouldBeTrue("Push should remain enabled");

        string statusText = await page.GetSettingsStatusTextAsync();
        statusText.ShouldContain("Analytics: False");
        statusText.ShouldContain("Marketing: True");
        statusText.ShouldContain("Push: True");
    }

    [Fact]
    public async Task ColoredSwitch_WhenClicked_ShouldToggle()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickBlueSwitchAsync();
        await Page!.WaitForBlazorAsync();

        // Assert
        bool isChecked = await page.IsSwitchCheckedAsync("switch-blue");
        isChecked.ShouldBeTrue("Blue switch should be checked");
    }

    [Fact]
    public async Task DisabledSwitch_ShouldBeDisabled()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Assert
        bool isDisabled = await page.IsSwitchDisabledAsync("disabled-switch");
        isDisabled.ShouldBeTrue("Disabled switch should be disabled");

        bool isChecked = await page.IsSwitchCheckedAsync("disabled-switch");
        isChecked.ShouldBeTrue("Disabled switch should be checked");
    }

    #endregion

    #region Dialog Tests

    [Fact]
    public async Task DialogSection_WhenLoaded_ShouldBeVisible()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        bool isVisible = await page.IsDialogSectionVisibleAsync();

        // Assert
        isVisible.ShouldBeTrue("Dialog section should be visible");
    }

    [Fact]
    public async Task SimpleDialog_WhenOpened_ShouldBeVisible()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickOpenSimpleDialogAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300); // Wait for animation

        // Assert
        bool isVisible = await page.IsSimpleDialogVisibleAsync();
        isVisible.ShouldBeTrue("Simple dialog should be visible after opening");
    }

    [Fact]
    public async Task SimpleDialog_WhenConfirmed_ShouldCloseAndUpdateResult()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();
        await page.ClickOpenSimpleDialogAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300);

        // Act
        await page.ClickSimpleDialogConfirmAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300); // Wait for close animation

        // Assert
        bool isVisible = await page.IsSimpleDialogVisibleAsync();
        isVisible.ShouldBeFalse("Dialog should be closed after confirm");

        string resultText = await page.GetDialogResultTextAsync();
        resultText.ShouldContain("Confirmed");
    }

    [Fact]
    public async Task SimpleDialog_WhenCancelled_ShouldCloseAndUpdateResult()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();
        await page.ClickOpenSimpleDialogAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300);

        // Act
        await page.ClickSimpleDialogCancelAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300);

        // Assert
        bool isVisible = await page.IsSimpleDialogVisibleAsync();
        isVisible.ShouldBeFalse("Dialog should be closed after cancel");

        string resultText = await page.GetDialogResultTextAsync();
        resultText.ShouldContain("Cancelled");
    }

    [Fact]
    public async Task LargeDialog_WhenOpened_ShouldBeVisible()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickOpenLargeDialogAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300);

        // Assert
        bool isVisible = await page.IsLargeDialogVisibleAsync();
        isVisible.ShouldBeTrue("Large dialog should be visible");
    }

    [Fact]
    public async Task LargeDialog_WhenClosed_ShouldClose()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();
        await page.ClickOpenLargeDialogAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300);

        // Act
        await page.ClickLargeDialogCloseAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300);

        // Assert
        bool isVisible = await page.IsLargeDialogVisibleAsync();
        isVisible.ShouldBeFalse("Large dialog should be closed");
    }

    [Fact]
    public async Task SmallDialog_WhenOpened_ShouldBeVisible()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();

        // Act
        await page.ClickOpenSmallDialogAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300);

        // Assert
        bool isVisible = await page.IsSmallDialogVisibleAsync();
        isVisible.ShouldBeTrue("Small dialog should be visible");
    }

    [Fact]
    public async Task SmallDialog_WhenOkClicked_ShouldClose()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();
        await page.ClickOpenSmallDialogAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300);

        // Act
        await page.ClickSmallDialogOkAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300);

        // Assert
        bool isVisible = await page.IsSmallDialogVisibleAsync();
        isVisible.ShouldBeFalse("Small dialog should be closed");
    }

    [Fact]
    public async Task Dialog_WhenEscapePressed_ShouldClose()
    {
        // Arrange
        ComponentShowcasePage page = new(Page!, BaseUrl);
        await page.NavigateAsync();
        await page.ClickOpenSimpleDialogAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300);

        // Act
        await page.PressEscapeKeyAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(300);

        // Assert
        bool isVisible = await page.IsSimpleDialogVisibleAsync();
        isVisible.ShouldBeFalse("Dialog should close on Escape key");
    }

    #endregion

    #region Accessibility Tests

    [Fact]
    public async Task AllComponents_WhenLoaded_ShouldHaveNoConsoleErrors()
    {
        // Arrange
        List<string> consoleErrors = new();
        Page!.Console += (_, msg) =>
        {
            if (msg.Type == "error" && !IsAcceptableError(msg.Text))
            {
                consoleErrors.Add(msg.Text);
            }
        };

        ComponentShowcasePage page = new(Page!, BaseUrl);

        // Act
        await page.NavigateAsync();
        await Page!.WaitForBlazorAsync();
        await Task.Delay(1000); // Wait for any delayed errors

        // Assert
        consoleErrors.ShouldBeEmpty($"No console errors expected. Errors: {string.Join(", ", consoleErrors)}");
    }

    private static bool IsAcceptableError(string message)
    {
        return message.Contains("favicon.ico") ||
               message.Contains(".map") ||
               message.Contains("sourcemap") ||
               message.Contains("404");
    }

    #endregion
}
