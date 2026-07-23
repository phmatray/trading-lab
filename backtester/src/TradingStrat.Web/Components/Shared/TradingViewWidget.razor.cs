using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TradingStrat.Web.Components.Shared;

public partial class TradingViewWidget : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    [Parameter]
    public string Ticker { get; set; } = "CON3.L";

    [Parameter]
    public string Theme { get; set; } = "dark";

    private readonly string _widgetId = Guid.NewGuid().ToString("N");

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await JS.InvokeVoidAsync("TradingView.loadChart", _widgetId, Ticker, Theme);
            }
            catch (Exception ex)
            {
                // Log error but don't break the page
                Console.WriteLine($"Error loading TradingView widget: {ex.Message}");
            }
        }
    }
}
