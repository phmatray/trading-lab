namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object for the Component Showcase page.
/// Provides access to Phase 2 UI components for E2E testing.
/// </summary>
public class ComponentShowcasePage(IPage page, string baseUrl) : BasePage(page, baseUrl)
{
    protected override string PagePath => "/component-showcase";

    // Sections
    private ILocator CheckboxSection => Page.Locator("[data-testid='checkbox-section']");
    private ILocator RadioSection => Page.Locator("[data-testid='radio-section']");
    private ILocator SwitchSection => Page.Locator("[data-testid='switch-section']");
    private ILocator DialogSection => Page.Locator("[data-testid='dialog-section']");

    // Checkbox elements
    private ILocator AcceptTermsCheckbox => Page.Locator("[data-testid='accept-terms-checkbox']");
    private ILocator NotificationsCheckbox => Page.Locator("[data-testid='enable-notifications']");
    private ILocator DarkModeCheckbox => Page.Locator("[data-testid='enable-darkmode']");
    private ILocator AutoSaveCheckbox => Page.Locator("[data-testid='enable-autosave']");
    private ILocator BlueCheckbox => Page.Locator("[data-testid='checkbox-blue']");
    private ILocator FeaturesStatus => Page.Locator("[data-testid='features-status']");

    // Radio elements
    private ILocator CardRadio => Page.Locator("[data-testid='radio-card']");
    private ILocator PayPalRadio => Page.Locator("[data-testid='radio-paypal']");
    private ILocator BankRadio => Page.Locator("[data-testid='radio-bank']");
    private ILocator PriorityLowRadio => Page.Locator("[data-testid='priority-low']");
    private ILocator PriorityHighRadio => Page.Locator("[data-testid='priority-high']");
    private ILocator PaymentStatus => Page.Locator("[data-testid='payment-status']");

    // Switch elements
    private ILocator FeatureSwitch => Page.Locator("[data-testid='feature-switch']");
    private ILocator AnalyticsSwitch => Page.Locator("[data-testid='analytics-switch']");
    private ILocator MarketingSwitch => Page.Locator("[data-testid='marketing-switch']");
    private ILocator PushSwitch => Page.Locator("[data-testid='push-switch']");
    private ILocator BlueSwitchEl => Page.Locator("[data-testid='switch-blue']");
    private ILocator SettingsStatus => Page.Locator("[data-testid='settings-status']");

    // Dialog elements
    private ILocator OpenSimpleDialogButton => Page.Locator("[data-testid='open-simple-dialog']");
    private ILocator OpenLargeDialogButton => Page.Locator("[data-testid='open-large-dialog']");
    private ILocator OpenSmallDialogButton => Page.Locator("[data-testid='open-small-dialog']");
    private ILocator DialogResult => Page.Locator("[data-testid='dialog-result']");
    private ILocator SimpleDialog => Page.Locator("[data-testid='simple-dialog']");
    private ILocator LargeDialog => Page.Locator("[data-testid='large-dialog']");
    private ILocator SmallDialog => Page.Locator("[data-testid='small-dialog']");
    private ILocator SimpleDialogCancel => Page.Locator("[data-testid='simple-dialog-cancel']");
    private ILocator SimpleDialogConfirm => Page.Locator("[data-testid='simple-dialog-confirm']");
    private ILocator LargeDialogClose => Page.Locator("[data-testid='large-dialog-close']");
    private ILocator SmallDialogOk => Page.Locator("[data-testid='small-dialog-ok']");

    // Checkbox methods
    public async Task<bool> IsCheckboxSectionVisibleAsync() =>
        await CheckboxSection.IsVisibleAsync();

    public async Task ClickAcceptTermsCheckboxAsync() =>
        await AcceptTermsCheckbox.ClickAsync();

    public async Task ClickNotificationsCheckboxAsync() =>
        await NotificationsCheckbox.ClickAsync();

    public async Task ClickDarkModeCheckboxAsync() =>
        await DarkModeCheckbox.ClickAsync();

    public async Task ClickAutoSaveCheckboxAsync() =>
        await AutoSaveCheckbox.ClickAsync();

    public async Task ClickBlueCheckboxAsync() =>
        await BlueCheckbox.ClickAsync();

    public async Task<bool> IsCheckboxCheckedAsync(string testId)
    {
        ILocator checkbox = Page.Locator($"[data-testid='{testId}'] input[type='checkbox']");
        return await checkbox.IsCheckedAsync();
    }

    public async Task<bool> IsCheckboxDisabledAsync(string testId)
    {
        ILocator checkbox = Page.Locator($"[data-testid='{testId}'] input[type='checkbox']");
        return await checkbox.IsDisabledAsync();
    }

    public async Task<string> GetFeaturesStatusTextAsync() =>
        await FeaturesStatus.TextContentAsync() ?? string.Empty;

    // Radio methods
    public async Task<bool> IsRadioSectionVisibleAsync() =>
        await RadioSection.IsVisibleAsync();

    public async Task ClickCardRadioAsync() =>
        await CardRadio.ClickAsync();

    public async Task ClickPayPalRadioAsync() =>
        await PayPalRadio.ClickAsync();

    public async Task ClickBankRadioAsync() =>
        await BankRadio.ClickAsync();

    public async Task ClickPriorityLowRadioAsync() =>
        await PriorityLowRadio.ClickAsync();

    public async Task ClickPriorityHighRadioAsync() =>
        await PriorityHighRadio.ClickAsync();

    public async Task<bool> IsRadioCheckedAsync(string testId)
    {
        ILocator radio = Page.Locator($"[data-testid='{testId}'] input[type='radio']");
        return await radio.IsCheckedAsync();
    }

    public async Task<string> GetPaymentStatusTextAsync() =>
        await PaymentStatus.TextContentAsync() ?? string.Empty;

    // Switch methods
    public async Task<bool> IsSwitchSectionVisibleAsync() =>
        await SwitchSection.IsVisibleAsync();

    public async Task ClickFeatureSwitchAsync() =>
        await FeatureSwitch.ClickAsync();

    public async Task ClickAnalyticsSwitchAsync() =>
        await AnalyticsSwitch.ClickAsync();

    public async Task ClickMarketingSwitchAsync() =>
        await MarketingSwitch.ClickAsync();

    public async Task ClickPushSwitchAsync() =>
        await PushSwitch.ClickAsync();

    public async Task ClickBlueSwitchAsync() =>
        await BlueSwitchEl.ClickAsync();

    public async Task<bool> IsSwitchCheckedAsync(string testId)
    {
        ILocator switchElement = Page.Locator($"[data-testid='{testId}'] input[type='checkbox']");
        return await switchElement.IsCheckedAsync();
    }

    public async Task<bool> IsSwitchDisabledAsync(string testId)
    {
        ILocator switchElement = Page.Locator($"[data-testid='{testId}'] input[type='checkbox']");
        return await switchElement.IsDisabledAsync();
    }

    public async Task<string> GetSettingsStatusTextAsync() =>
        await SettingsStatus.TextContentAsync() ?? string.Empty;

    // Dialog methods
    public async Task<bool> IsDialogSectionVisibleAsync() =>
        await DialogSection.IsVisibleAsync();

    public async Task ClickOpenSimpleDialogAsync() =>
        await OpenSimpleDialogButton.ClickAsync();

    public async Task ClickOpenLargeDialogAsync() =>
        await OpenLargeDialogButton.ClickAsync();

    public async Task ClickOpenSmallDialogAsync() =>
        await OpenSmallDialogButton.ClickAsync();

    public async Task<bool> IsSimpleDialogVisibleAsync() =>
        await SimpleDialog.IsVisibleAsync();

    public async Task<bool> IsLargeDialogVisibleAsync() =>
        await LargeDialog.IsVisibleAsync();

    public async Task<bool> IsSmallDialogVisibleAsync() =>
        await SmallDialog.IsVisibleAsync();

    public async Task ClickSimpleDialogCancelAsync() =>
        await SimpleDialogCancel.ClickAsync();

    public async Task ClickSimpleDialogConfirmAsync() =>
        await SimpleDialogConfirm.ClickAsync();

    public async Task ClickLargeDialogCloseAsync() =>
        await LargeDialogClose.ClickAsync();

    public async Task ClickSmallDialogOkAsync() =>
        await SmallDialogOk.ClickAsync();

    public async Task<string> GetDialogResultTextAsync() =>
        await DialogResult.TextContentAsync() ?? string.Empty;

    public async Task PressEscapeKeyAsync() =>
        await Page.Keyboard.PressAsync("Escape");

    public async Task ClickBackdropAsync()
    {
        // Click on the backdrop (outside dialog)
        ILocator backdrop = Page.Locator("div[data-state='open']").First;
        await backdrop.ClickAsync(new LocatorClickOptions { Position = new Position { X = 0, Y = 0 } });
    }
}
