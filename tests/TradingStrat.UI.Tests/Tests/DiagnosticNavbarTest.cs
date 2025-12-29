using Xunit.Abstractions;

namespace TradingStrat.UI.Tests.Tests;

public class DiagnosticNavbarTest : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DiagnosticNavbarTest(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture, ITestOutputHelper testOutputHelper)
        : base(playwrightFixture, appFixture)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task DiagnoseNavbar_ShouldPrintPageStructure()
    {
        // Navigate to page
        await Page!.GotoAsync($"{BaseUrl}/phase3-component-showcase");
        await Page!.WaitForBlazorAsync();
        await Page!.WaitForTimeoutAsync(2000);

        // Get all data-testid elements
        IReadOnlyList<ILocator> testIds = await Page!.Locator("[data-testid]").AllAsync();
        _testOutputHelper.WriteLine($"Found {testIds.Count} elements with data-testid:");
        foreach (ILocator element in testIds)
        {
            string? testId = await element.GetAttributeAsync("data-testid");
            bool isVisible = await element.IsVisibleAsync();
            _testOutputHelper.WriteLine($"  - {testId}: visible={isVisible}");
        }

        // Try to find navbar section
        bool navbarSectionVisible = await Page!.Locator("[data-testid='navbar-section']").IsVisibleAsync();
        _testOutputHelper.WriteLine($"\nNavbar section visible: {navbarSectionVisible}");

        // Try to find current-nav-page
        bool currentNavPageExists = await Page!.Locator("[data-testid='current-nav-page']").CountAsync() > 0;
        bool currentNavPageVisible = currentNavPageExists && await Page!.Locator("[data-testid='current-nav-page']").IsVisibleAsync();
        _testOutputHelper.WriteLine($"Current nav page exists: {currentNavPageExists}");
        _testOutputHelper.WriteLine($"Current nav page visible: {currentNavPageVisible}");

        if (currentNavPageExists)
        {
            string? text = await Page!.Locator("[data-testid='current-nav-page']").TextContentAsync();
            _testOutputHelper.WriteLine($"Current nav page text: '{text}'");
        }

        // Print HTML around navbar section
        if (navbarSectionVisible)
        {
            string navbarHtml = await Page!.Locator("[data-testid='navbar-section']").InnerHTMLAsync();
            _testOutputHelper.WriteLine($"\nNavbar section HTML (first 500 chars):");
            _testOutputHelper.WriteLine(navbarHtml.Substring(0, Math.Min(500, navbarHtml.Length)));
        }
    }
}
