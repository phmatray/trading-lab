namespace TradingStrat.UI.Tests.Tests;

public class DiagnosticNavbarTest : BaseTest
{
    public DiagnosticNavbarTest(PlaywrightFixture playwrightFixture, WebApplicationFixture appFixture)
        : base(playwrightFixture, appFixture)
    {
    }

    [Fact]
    public async Task DiagnoseNavbar_ShouldPrintPageStructure()
    {
        // Navigate to page
        await Page!.GotoAsync($"{BaseUrl}/phase3-component-showcase");
        await Page!.WaitForBlazorAsync();
        await Page!.WaitForTimeoutAsync(2000);

        // Get all data-testid elements
        var testIds = await Page!.Locator("[data-testid]").AllAsync();
        Console.WriteLine($"Found {testIds.Count} elements with data-testid:");
        foreach (var element in testIds)
        {
            string? testId = await element.GetAttributeAsync("data-testid");
            bool isVisible = await element.IsVisibleAsync();
            Console.WriteLine($"  - {testId}: visible={isVisible}");
        }

        // Try to find navbar section
        bool navbarSectionVisible = await Page!.Locator("[data-testid='navbar-section']").IsVisibleAsync();
        Console.WriteLine($"\nNavbar section visible: {navbarSectionVisible}");

        // Try to find current-nav-page
        bool currentNavPageExists = await Page!.Locator("[data-testid='current-nav-page']").CountAsync() > 0;
        bool currentNavPageVisible = currentNavPageExists && await Page!.Locator("[data-testid='current-nav-page']").IsVisibleAsync();
        Console.WriteLine($"Current nav page exists: {currentNavPageExists}");
        Console.WriteLine($"Current nav page visible: {currentNavPageVisible}");

        if (currentNavPageExists)
        {
            string? text = await Page!.Locator("[data-testid='current-nav-page']").TextContentAsync();
            Console.WriteLine($"Current nav page text: '{text}'");
        }

        // Print HTML around navbar section
        if (navbarSectionVisible)
        {
            string? navbarHtml = await Page!.Locator("[data-testid='navbar-section']").InnerHTMLAsync();
            Console.WriteLine($"\nNavbar section HTML (first 500 chars):");
            if (navbarHtml != null)
            {
                Console.WriteLine(navbarHtml.Substring(0, Math.Min(500, navbarHtml.Length)));
            }
        }
    }
}
