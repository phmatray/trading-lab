namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for the Strategy Builder page (/strategies/builder).
/// Tests custom strategy creation, editing, and validation.
/// </summary>
public class StrategyBuilderPageTests : BaseTest
{
    public StrategyBuilderPageTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task StrategyBuilder_WhenLoaded_ShouldDisplayTitle()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/builder");

        // Assert
        ILocator title = Page!.Locator("h1:has-text('Create Custom Strategy')");
        await title.ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyBuilder_WhenLoaded_ShouldShowAllFormSections()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/builder");

        // Assert
        await Page!.Locator("text=Strategy Information").ShouldBeVisibleAsync();
        await Page!.Locator("text=Position Sizing").ShouldBeVisibleAsync();
        await Page!.Locator("text=Entry Rules").ShouldBeVisibleAsync();
        await Page!.Locator("text=Exit Rules").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyBuilder_CreateButtonClick_ShouldNavigateToBuilder()
    {
        // Arrange
        await NavigateToAsync("/strategies/library");

        // Act
        await Page!.Locator("button:has-text('Create Strategy')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.WaitForURLAsync("**/strategies/builder");
        Page.Url.ShouldContain("/strategies/builder");
    }

    [Fact]
    public async Task StrategyBuilder_EmptyEntryRules_ShouldShowEmptyState()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/builder");

        // Assert
        ILocator emptyState = Page!.Locator("text=No entry rules defined");
        await emptyState.ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyBuilder_AddEntryRule_ShouldDisplayRuleInputs()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act
        await Page!.Locator("button:has-text('Add First Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.Locator("[data-testid='rule-0-indicator']").ShouldBeVisibleAsync();
        await Page.Locator("text=Indicator").ShouldBeVisibleAsync();
        await Page.Locator("text=Operator").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyBuilder_SelectIndicator_ShouldShowIndicatorOptions()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("button:has-text('Add First Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Act
        ILocator indicatorSelect = Page.Locator("[data-testid='rule-0-indicator']");
        await indicatorSelect.ClickAsync();

        // Assert - Check for indicator categories
        await Page.Locator("optgroup[label='Momentum']").ShouldBeVisibleAsync();
        await Page.Locator("option:has-text('RSI')").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyBuilder_SelectRSIIndicator_ShouldShowPeriodParameter()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("button:has-text('Add First Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Act
        await Page.Locator("[data-testid='rule-0-indicator']").SelectOptionAsync("RSI");
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.Locator("text=Period").ShouldBeVisibleAsync();
        ILocator periodInput = Page.Locator("input[type='number']").First;
        string value = await periodInput.InputValueAsync();
        value.ShouldBe("14"); // Default RSI period
    }

    [Fact]
    public async Task StrategyBuilder_ComparisonOperators_ShouldShowAllOptions()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("button:has-text('Add First Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Act
        ILocator operatorSelect = Page.Locator("[data-testid='rule-0-operator']"); // Second select is operator
        await operatorSelect.ClickAsync();

        // Assert
        await Page.Locator("option:has-text('>')").ShouldBeVisibleAsync();
        await Page.Locator("option:has-text('<')").ShouldBeVisibleAsync();
        await Page.Locator("option:has-text('Crosses Above')").ShouldBeVisibleAsync();
        await Page.Locator("option:has-text('Crosses Below')").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyBuilder_ValueType_ShouldShowNumberPriceIndicator()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("button:has-text('Add First Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Act
        ILocator valueTypeSelect = Page.Locator("text=Compare To").Locator("..").Locator("select");
        await valueTypeSelect.ClickAsync();

        // Assert
        await Page.Locator("option:has-text('Number')").ShouldBeVisibleAsync();
        await Page.Locator("option:has-text('Price')").ShouldBeVisibleAsync();
        await Page.Locator("option:has-text('Indicator')").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyBuilder_AddMultipleRules_ShouldShowANDORLogic()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act - Add first rule
        await Page!.Locator("button:has-text('Add First Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Add second rule
        await Page.Locator("button:has-text('Add Another Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should show AND/OR radio buttons
        await Page.Locator("input[type='radio'][value='And']").ShouldBeVisibleAsync();
        await Page.Locator("input[type='radio'][value='Or']").ShouldBeVisibleAsync();
        await Page.Locator("span.font-semibold:has-text('AND')").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyBuilder_RemoveRule_ShouldWork()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("button:has-text('Add First Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();

        // Act
        await Page.Locator("button[title='Remove rule']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should show empty state again
        await Page.Locator("text=No entry rules defined").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyBuilder_PositionSizingModes_ShouldShowAllOptions()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/builder");
        ILocator sizingSelect = Page!.Locator("text=Sizing Mode").Locator("..").Locator("select");
        await sizingSelect.ClickAsync();

        // Assert
        await Page.Locator("option:has-text('Fixed Percentage')").ShouldBeVisibleAsync();
        await Page.Locator("option:has-text('Fixed Quantity')").ShouldBeVisibleAsync();
        await Page.Locator("option:has-text('Risk-Based')").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyBuilder_SubmitWithoutRules_ShouldShowValidation()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Fill in basic info
        await Page!.Locator("input[id='Name']").FillAsync("Test Strategy");
        await Page.Locator("input[id='Author']").FillAsync("Test Author");

        // Act - Submit without adding rules
        await Page.Locator("button[type='submit']:has-text('Create Strategy')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should show validation errors
        await Page.Locator("text=At least one entry rule is required").ShouldBeVisibleAsync();
        await Page.Locator("text=At least one exit rule is required").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task StrategyBuilder_CreateCompleteStrategy_ShouldSucceed()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        string strategyName = $"E2E Test Strategy {DateTime.Now.Ticks}";

        // Act - Fill strategy info
        await Page!.Locator("#Name").FillAsync(strategyName);
        await Page.Locator("#Author").FillAsync("E2E Test");
        await Page.Locator("#Category").FillAsync("Test");
        await Page.Locator("textarea").FillAsync("E2E test strategy description");

        // Add entry rule: RSI < 30
        await Page.Locator("button:has-text('Add First Rule')").First.ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.Locator("[data-testid='rule-0-indicator']").SelectOptionAsync("RSI");
        await Page.WaitForBlazorAsync();
        await Page.Locator("[data-testid='rule-0-operator']").SelectOptionAsync("LessThan");
        await Page.Locator("[data-testid='rule-0-value']").FillAsync("30");

        // Add exit rule: RSI > 70
        ILocator exitSection = Page.Locator(".card:has(h2:has-text('Exit Rules'))");
        await exitSection.Locator("button:has-text('Add First Rule')").ClickAsync();
        await Page.WaitForBlazorAsync();

        await exitSection.Locator("[data-testid='rule-0-indicator']").SelectOptionAsync("RSI");
        await Page.WaitForBlazorAsync();
        await exitSection.Locator("[data-testid='rule-0-operator']").SelectOptionAsync("GreaterThan");
        await exitSection.Locator("[data-testid='rule-0-value']").FillAsync("70");

        // Submit
        await Page.Locator("button[type='submit']:has-text('Create Strategy')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should navigate to library
        await Page.WaitForURLAsync("**/strategies/library");
        Page.Url.ShouldContain("/strategies/library");
    }

    [Fact]
    public async Task StrategyBuilder_CancelButton_ShouldNavigateToLibrary()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act
        await Page!.Locator("button:has-text('Cancel')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.WaitForURLAsync("**/strategies/library");
        Page.Url.ShouldContain("/strategies/library");
    }

    [Fact]
    public async Task StrategyBuilder_BackToLibraryButton_ShouldNavigate()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act
        await Page!.Locator("button:has-text('Back to Library')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.WaitForURLAsync("**/strategies/library");
        Page.Url.ShouldContain("/strategies/library");
    }

    // ======================
    // Python Strategy Tests
    // ======================

    [Fact]
    public async Task PythonStrategy_WhenLoaded_ShouldShowStrategyTypeSelector()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/builder");

        // Assert
        await Page!.Locator("text=Strategy Type").ShouldBeVisibleAsync();
        await Page.Locator("text=Rule-Based Strategy").ShouldBeVisibleAsync();
        await Page.Locator("text=Python Code Strategy").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task PythonStrategy_SelectPythonType_ShouldShowMonacoEditor()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act - Select Python strategy type
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.Locator("text=Python Strategy Code").ShouldBeVisibleAsync();
        await Page.Locator(".monaco-editor-container").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task PythonStrategy_SelectPythonType_ShouldHideRuleEditor()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act - Select Python strategy type
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Rule sections should not be visible
        int entryRulesCount = await Page.Locator("text=Entry Rules").CountAsync();
        int exitRulesCount = await Page.Locator("text=Exit Rules").CountAsync();
        int positionSizingCount = await Page.Locator("text=Position Sizing").CountAsync();

        entryRulesCount.ShouldBe(0);
        exitRulesCount.ShouldBe(0);
        positionSizingCount.ShouldBe(0);
    }

    [Fact]
    public async Task PythonStrategy_ShouldShowValidateSyntaxButton()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.Locator("button:has-text('Validate Syntax')").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task PythonStrategy_ShouldShowDryRunButton()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.Locator("button:has-text('Dry Run')").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task PythonStrategy_ShouldShowAllowedLibraries()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.Locator("text=Allowed Libraries:").ShouldBeVisibleAsync();
        await Page.Locator("text=talib").ShouldBeVisibleAsync();
        await Page.Locator("text=numpy").ShouldBeVisibleAsync();
        await Page.Locator("text=pandas").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task PythonStrategy_ShouldShowSecurityNotice()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.Locator("text=Security:").ShouldBeVisibleAsync();
        await Page.Locator("text=Network and file system access are disabled").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task PythonStrategy_SubmitWithEmptyCode_ShouldShowValidation()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Fill in basic info
        await Page.Locator("#Name").FillAsync("Python Test Strategy");
        await Page.Locator("#Author").FillAsync("Test Author");
        await Page.Locator("#Category").FillAsync("Test");

        // Act - Submit without Python code (assuming Monaco editor starts empty or we clear it)
        await Page.Locator("button[type='submit']:has-text('Create Strategy')").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should show validation error
        await Page.Locator("text=Python code is required").ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task PythonStrategy_DefaultToRuleBasedType()
    {
        // Arrange & Act
        await NavigateToAsync("/strategies/builder");

        // Assert - Rule-Based should be selected by default
        ILocator ruleBasedRadio = Page!.Locator("input[type='radio'][value='RuleBased']");
        (await ruleBasedRadio.IsCheckedAsync()).ShouldBeTrue();
    }

    [Fact]
    public async Task PythonStrategy_SwitchBetweenTypes_ShouldUpdateUI()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act - Switch to Python
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Monaco editor visible
        await Page.Locator(".monaco-editor-container").ShouldBeVisibleAsync();

        // Act - Switch back to Rule-Based
        await Page.Locator("input[type='radio'][value='RuleBased']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Rule editor visible again
        await Page.Locator("text=Position Sizing").ShouldBeVisibleAsync();
        await Page.Locator("text=Entry Rules").ShouldBeVisibleAsync();

        // Monaco editor should be hidden
        int monacoCount = await Page.Locator(".monaco-editor-container").CountAsync();
        monacoCount.ShouldBe(0);
    }

    [Fact]
    public async Task PythonStrategy_MonacoEditorContainer_ShouldHaveUniqueId()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        ILocator container = Page.Locator(".monaco-editor-container");
        await container.ShouldBeVisibleAsync();

        string? id = await container.GetAttributeAsync("id");
        id.ShouldNotBeNullOrEmpty();
        id.ShouldStartWith("monaco-editor-");
    }

    [Fact]
    public async Task PythonStrategy_RequiredFunction_ShouldBeDocumented()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert - Should show documentation about required function
        await Page.Locator("text=generate_signal").ShouldBeVisibleAsync();
        await Page.Locator("code:has-text('generate_signal(index, price, cash, position)')").ShouldBeVisibleAsync();
    }
}
