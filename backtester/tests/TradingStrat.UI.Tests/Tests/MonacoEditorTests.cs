namespace TradingStrat.UI.Tests.Tests;

/// <summary>
/// E2E tests for Monaco Editor integration in Python strategies.
/// Verifies that Monaco actually loads and is functional, not just div visibility.
/// </summary>
[Collection("Playwright")]
public class MonacoEditorTests : BaseTest
{
    public MonacoEditorTests(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task MonacoEditor_WhenPythonSelected_ShouldLoadAndRender()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.WaitForMonacoAsync();

        // Assert - Container exists
        ILocator container = Page.Locator(".monaco-editor-container");
        await container.ShouldBeVisibleAsync();

        // Assert - Monaco editor actually rendered (not just container div)
        ILocator monacoEditor = Page.Locator(".monaco-editor");
        await monacoEditor.ShouldBeVisibleAsync();

        // Assert - Code editor lines are visible
        ILocator linesContent = Page.Locator(".lines-content");
        await linesContent.ShouldBeVisibleAsync();
    }

    [Fact]
    public async Task MonacoEditor_ShouldLoadDefaultPythonTemplate()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.WaitForMonacoAsync();

        // Act - Get code via JavaScript
        string code = await Page.EvaluateAsync<string>(
            "() => window.monacoEditorHelper.getCode()");

        // Assert
        code.ShouldNotBeNullOrEmpty();
        code.ShouldContain("import talib");
        code.ShouldContain("def generate_signal");
        code.ShouldContain("def initialize");
    }

    [Fact]
    public async Task MonacoEditor_ShouldBeEditable()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.WaitForMonacoAsync();

        const string testCode = "# Test code\nprint('hello')";

        // Act - Set code via JavaScript
        await Page.EvaluateAsync($"() => window.monacoEditorHelper.setCode(`{testCode}`)");
        await Task.Delay(500); // Wait for editor update

        // Assert - Read back
        string actualCode = await Page.EvaluateAsync<string>(
            "() => window.monacoEditorHelper.getCode()");
        actualCode.ShouldBe(testCode);
    }

    [Fact]
    public async Task MonacoEditor_ShouldNotGenerateConsoleErrors()
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

        // Act
        await NavigateToAsync("/strategies/builder");
        await Page.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.WaitForMonacoAsync();
        await Task.Delay(1000); // Wait for delayed errors

        // Assert
        consoleErrors.ShouldBeEmpty(
            $"Unexpected console errors: {string.Join(", ", consoleErrors)}");
    }

    [Fact]
    public async Task MonacoEditor_StaticFiles_ShouldBeAccessible()
    {
        // Act - Navigate directly to Monaco loader
        IResponse? response = await Page!.GotoAsync(
            $"{AppFixture.BaseAddress}/node_modules/monaco-editor/min/vs/loader.js");

        // Assert
        response.ShouldNotBeNull();
        response.Status.ShouldBe(200);

        string contentType = response.Headers["content-type"];
        contentType.ShouldContain("javascript", Case.Insensitive);
    }

    [Fact]
    public async Task MonacoEditor_SwitchingBetweenTypes_ShouldWorkCorrectly()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");

        // Act - Switch to Python
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.WaitForMonacoAsync();

        await Page.Locator(".monaco-editor").ShouldBeVisibleAsync();

        // Act - Switch to RuleBased
        await Page.Locator("input[type='radio'][value='RuleBased']").ClickAsync();
        await Page.WaitForBlazorAsync();

        int monacoCount = await Page.Locator(".monaco-editor").CountAsync();
        monacoCount.ShouldBe(0);

        // Act - Switch back to Python
        await Page.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.WaitForMonacoAsync();

        // Assert - Should work again
        await Page.Locator(".monaco-editor").ShouldBeVisibleAsync();

        bool hasCode = await Page.EvaluateAsync<bool>(
            "() => window.monacoEditorHelper && window.monacoEditorHelper.getCode().length > 0");
        hasCode.ShouldBeTrue();
    }

    [Fact]
    public async Task MonacoEditor_ValidateSyntaxButton_ShouldBeEnabled()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.WaitForMonacoAsync();

        // Assert
        ILocator button = Page.Locator("button:has-text('Validate Syntax')");
        await button.ShouldBeVisibleAsync();
        await button.ShouldBeEnabledAsync();
    }

    [Fact]
    public async Task MonacoEditor_DryRunButton_ShouldBeEnabled()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.WaitForMonacoAsync();

        // Assert
        ILocator button = Page.Locator("button:has-text('Dry Run')");
        await button.ShouldBeVisibleAsync();
        await button.ShouldBeEnabledAsync();
    }

    [Fact]
    public async Task MonacoEditor_ShouldHaveUniqueContainerId()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();
        await Page.WaitForMonacoAsync();

        // Act
        ILocator container = Page.Locator(".monaco-editor-container");
        string? id = await container.GetAttributeAsync("id");

        // Assert
        id.ShouldNotBeNullOrEmpty();
        id.ShouldStartWith("monaco-editor-");
        id!.Length.ShouldBeGreaterThan(20); // GUID-based
    }

    [Fact]
    public async Task MonacoEditor_Documentation_ShouldBeVisible()
    {
        // Arrange
        await NavigateToAsync("/strategies/builder");
        await Page!.Locator("input[type='radio'][value='Python']").ClickAsync();
        await Page.WaitForBlazorAsync();

        // Assert
        await Page.Locator("text=Python Strategy Code").ShouldBeVisibleAsync();
        await Page.Locator("text=Allowed Libraries:").ShouldBeVisibleAsync();

        // Use more specific selectors to avoid matching Monaco editor content
        await Page.Locator(".px-2.py-1:has-text('talib')").First.ShouldBeVisibleAsync();
        await Page.Locator(".px-2.py-1:has-text('numpy')").First.ShouldBeVisibleAsync();
        await Page.Locator(".px-2.py-1:has-text('pandas')").First.ShouldBeVisibleAsync();
        await Page.Locator("code:has-text('generate_signal')").ShouldBeVisibleAsync();
    }

    private static bool IsAcceptableError(string message)
    {
        return message.Contains("favicon.ico") ||
               message.Contains(".map") ||
               message.Contains("sourcemap") ||
               message.Contains("404");
    }
}
