using Microsoft.AspNetCore.Components;

namespace TradyStrat.Features.Dashboard.Components;

public partial class CallDiffList : ComponentBase
{
    [Parameter, EditorRequired] public IReadOnlyList<CallDiffRow> Rows { get; set; } = [];

    private static string GlyphFor(string kind) => kind switch
    {
        "added"   => "+",
        "removed" => "−",
        _         => "Δ"
    };
}
