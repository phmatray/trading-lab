using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace TradingViewBlazor.Abstractions;

public abstract class TradingViewComponentBase<TSettings>
    : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = default!;
    
    [Parameter]
    [EditorRequired]
    public required TSettings Settings { get; set; }

    [Parameter]
    public bool ShowCopyright { get; set; }
    
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? WrapperAttributes { get; set; }
    
    protected abstract string JavaScriptFileName { get; }
    
    protected ElementReference WrapperReference { get; set; }

    private Lazy<Task<IJSObjectReference>> ModuleTask
        => new(() => JSRuntime
            .InvokeAsync<IJSObjectReference>("import", JavaScriptFileName)
            .AsTask());


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitTradingViewComponent();
        }
    }
    
    private async Task InitTradingViewComponent()
    {
        var module = await ModuleTask.Value;
        await module.InvokeVoidAsync("initTradingViewComponent", WrapperReference, Settings);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Produces:
        // <div @attributes="@WrapperAttributes">
        //   <div @ref="@WrapperReference" class="tradingview-widget-container">
        //     <div class="tradingview-widget-container__widget"></div>
        //     <WidgetCopyright ShowCopyright="@ShowCopyright" />
        //   </div>
        // </div>
        
        builder.OpenElement(0, "div");
        builder.AddMultipleAttributes(1, WrapperAttributes);
        
          builder.OpenElement(2, "div");
          builder.AddAttribute(3, "class", "tradingview-widget-container");
          builder.AddElementReferenceCapture(4, reference => WrapperReference = reference);
          
            builder.OpenElement(5, "div");
            builder.AddAttribute(6, "class", "tradingview-widget-container__widget");
            builder.CloseElement();
          
            builder.OpenComponent<TradingViewCopyright>(7);
            builder.AddAttribute(8, "ShowCopyright", ShowCopyright);
            builder.CloseComponent();
            
          builder.CloseElement();
          
        builder.CloseElement();
    }

    public async ValueTask DisposeAsync()
    {
        if (ModuleTask.IsValueCreated)
        {
            var module = await ModuleTask.Value;
            await module.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}