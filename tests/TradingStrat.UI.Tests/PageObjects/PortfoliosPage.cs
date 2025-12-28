namespace TradingStrat.UI.Tests.PageObjects;

/// <summary>
/// Page Object Model for the Portfolios page (/portfolios).
/// Represents the portfolio list view with create and delete functionality.
/// </summary>
public class PortfoliosPage : BasePage
{
    public PortfoliosPage(IPage page, string baseUrl) : base(page, baseUrl)
    {
    }

    protected override string PagePath => "/portfolios";

    // Page Elements
    private ILocator PageTitle => Page.Locator("main h1");
    private ILocator CreatePortfolioButton => Page.Locator("main button:has-text('Create Portfolio')").First;
    private ILocator PortfolioGrid => Page.Locator(".grid");
    private ILocator PortfolioCards => Page.Locator(".grid .card");

    // Create Portfolio Dialog
    private ILocator CreateDialog => Page.Locator("[data-testid='dialog']");
    private ILocator NameInput => CreateDialog.Locator("#name");
    private ILocator DescriptionTextarea => CreateDialog.Locator("#description");
    private ILocator InitialCashInput => CreateDialog.Locator("#initialCash");
    private ILocator CreateSubmitButton => CreateDialog.Locator("button[type='submit']");
    private ILocator CancelButton => CreateDialog.Locator("button[type='button']:has-text('Cancel')");

    // Delete Confirmation Dialog
    private ILocator DeleteDialog => Page.Locator("[data-testid='dialog']");
    private ILocator ConfirmDeleteButton => DeleteDialog.Locator("button:has-text('Delete')");
    private ILocator CancelDeleteButton => DeleteDialog.Locator("button:has-text('Cancel')");

    /// <summary>
    /// Gets the page title text.
    /// </summary>
    public async Task<string?> GetPageTitleAsync()
    {
        return await PageTitle.TextContentAsync();
    }

    /// <summary>
    /// Gets the count of portfolio cards displayed.
    /// </summary>
    public async Task<int> GetPortfolioCountAsync()
    {
        return await PortfolioCards.CountAsync();
    }

    /// <summary>
    /// Checks if the portfolio grid is visible.
    /// </summary>
    public async Task<bool> IsPortfolioGridVisibleAsync()
    {
        return await PortfolioGrid.IsVisibleAsync();
    }

    /// <summary>
    /// Clicks the Create Portfolio button to open the dialog.
    /// </summary>
    public async Task ClickCreatePortfolioButtonAsync()
    {
        await CreatePortfolioButton.ClickAsync();
        await Task.Delay(300); // Wait for dialog animation
    }

    /// <summary>
    /// Checks if the create portfolio dialog is visible.
    /// </summary>
    public async Task<bool> IsCreateDialogVisibleAsync()
    {
        return await CreateDialog.IsVisibleAsync();
    }

    /// <summary>
    /// Fills the create portfolio form and submits.
    /// </summary>
    public async Task CreatePortfolioAsync(string name, string? description = null, decimal initialCash = 10000m)
    {
        await ClickCreatePortfolioButtonAsync();

        await NameInput.FillAsync(name);

        if (description is not null)
        {
            await DescriptionTextarea.FillAsync(description);
        }

        await InitialCashInput.FillAsync(initialCash.ToString());
        await CreateSubmitButton.ClickAsync();

        await Page.WaitForBlazorAsync();
        await Task.Delay(500); // Wait for creation and UI update
    }

    /// <summary>
    /// Cancels the create portfolio dialog.
    /// </summary>
    public async Task CancelCreateDialogAsync()
    {
        await CancelButton.ClickAsync();
        await Task.Delay(300); // Wait for dialog close
    }

    /// <summary>
    /// Gets a portfolio card by name.
    /// </summary>
    public ILocator GetPortfolioCardByName(string portfolioName)
    {
        return Page.Locator($".card:has-text('{portfolioName}')");
    }

    /// <summary>
    /// Clicks a portfolio card to navigate to the dashboard.
    /// </summary>
    public async Task ClickPortfolioCardAsync(string portfolioName)
    {
        ILocator card = GetPortfolioCardByName(portfolioName);
        await card.ClickAsync();
        await Page.WaitForBlazorAsync();
    }

    /// <summary>
    /// Checks if a portfolio card exists.
    /// </summary>
    public async Task<bool> HasPortfolioCardAsync(string portfolioName)
    {
        ILocator card = GetPortfolioCardByName(portfolioName);
        return await card.IsVisibleAsync();
    }

    /// <summary>
    /// Gets the delete button for a specific portfolio card.
    /// </summary>
    private ILocator GetDeleteButton(string portfolioName)
    {
        return GetPortfolioCardByName(portfolioName).Locator("button:has-text('Delete')");
    }

    /// <summary>
    /// Clicks the delete button for a portfolio.
    /// </summary>
    public async Task ClickDeleteButtonAsync(string portfolioName)
    {
        ILocator deleteButton = GetDeleteButton(portfolioName);
        await deleteButton.ClickAsync();
        await Task.Delay(300); // Wait for confirmation dialog
    }

    /// <summary>
    /// Confirms the delete action in the confirmation dialog.
    /// </summary>
    public async Task ConfirmDeleteAsync()
    {
        await ConfirmDeleteButton.ClickAsync();
        await Page.WaitForBlazorAsync();
        await Task.Delay(500); // Wait for deletion and UI update
    }

    /// <summary>
    /// Cancels the delete action in the confirmation dialog.
    /// </summary>
    public async Task CancelDeleteAsync()
    {
        await CancelDeleteButton.ClickAsync();
        await Task.Delay(300); // Wait for dialog close
    }

    /// <summary>
    /// Deletes a portfolio (click delete button and confirm).
    /// </summary>
    public async Task DeletePortfolioAsync(string portfolioName)
    {
        await ClickDeleteButtonAsync(portfolioName);
        await ConfirmDeleteAsync();
    }

    /// <summary>
    /// Gets the cash value displayed on a portfolio card.
    /// </summary>
    public async Task<string?> GetPortfolioCashAsync(string portfolioName)
    {
        ILocator card = GetPortfolioCardByName(portfolioName);
        ILocator cashElement = card.Locator("p:has-text('Cash')");
        return await cashElement.TextContentAsync();
    }

    /// <summary>
    /// Gets the positions count displayed on a portfolio card.
    /// </summary>
    public async Task<string?> GetPortfolioPositionsCountAsync(string portfolioName)
    {
        ILocator card = GetPortfolioCardByName(portfolioName);
        ILocator positionsElement = card.Locator("p:has-text('Positions')");
        return await positionsElement.TextContentAsync();
    }

    /// <summary>
    /// Navigates to the Portfolios page via the navigation menu.
    /// </summary>
    public async Task NavigateViaMenuAsync()
    {
        await base.NavigateViaMenuAsync("Portfolios");
    }
}
