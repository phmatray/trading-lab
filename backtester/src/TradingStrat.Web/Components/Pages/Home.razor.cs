using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Pages;

/// <summary>
/// Home page component that redirects to the dashboard.
/// </summary>
public partial class Home : ComponentBase
{
    #region Dependency Injection

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    #endregion

    #region Lifecycle Methods

    protected override void OnInitialized()
    {
        // Redirect to dashboard on page load
        Navigation.NavigateTo("/dashboard", replace: true);
    }

    #endregion
}
