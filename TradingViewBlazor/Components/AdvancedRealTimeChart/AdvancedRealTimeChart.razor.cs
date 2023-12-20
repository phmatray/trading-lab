using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TradingViewBlazor.Components.AdvancedRealTimeChart;

public partial class AdvancedRealTimeChart
{
    private const string JavaScriptFileName = "./_content/TradingViewBlazor/Components/AdvancedRealTimeChart/AdvancedRealTimeChart.razor.js";
    
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;
    
    private IJSObjectReference? Module { get; set; }
    private ElementReference _advancedRealTimeWidgetReference;
    
    [Parameter]
    [EditorRequired]
    public required AdvancedRealTimeChartSettings Settings { get; set; }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", JavaScriptFileName);
            await Module.InvokeVoidAsync("initAdvancedRealTimeChart", _advancedRealTimeWidgetReference, Settings);
        }
    }
}