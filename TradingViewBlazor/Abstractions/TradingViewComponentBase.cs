using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TradingViewBlazor.Abstractions;

public abstract class TradingViewComponentBase<TSettings>
    : ComponentBase
{
    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;
    
    [Parameter]
    [EditorRequired]
    public required TSettings Settings { get; set; }
    
    protected abstract string JavaScriptFileName { get; }
    
    protected ElementReference WrapperReference { get; set; }
    
    private IJSObjectReference? Module { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Module ??= await JsRuntime.InvokeAsync<IJSObjectReference>("import", JavaScriptFileName);
            await Module.InvokeVoidAsync("initTradingViewComponent", WrapperReference, Settings);
        }
    }
}