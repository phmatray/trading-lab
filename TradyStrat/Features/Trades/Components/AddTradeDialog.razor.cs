using Microsoft.AspNetCore.Components;
using TradyStrat.Features.Trades.UseCases;
using TradyStrat.Common.Domain;

namespace TradyStrat.Features.Trades.Components;

public partial class AddTradeDialog : ComponentBase
{
    [Parameter] public Trade? Initial { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<LogTradeInput> OnSubmit { get; set; }

    private DateTime _date = DateTime.Today;
    private string _side = "Buy";
    private decimal _qty;
    private decimal _price;
    private decimal _fees;
    private string? _note;
    private string? _err;

    protected override void OnInitialized()
    {
        if (Initial is { } t)
        {
            _date  = t.ExecutedOn.ToDateTime(TimeOnly.MinValue);
            _side  = t.Side.ToString();
            _qty   = t.Quantity;
            _price = t.PricePerShare;
            _fees  = t.FeesEur;
            _note  = t.Note;
        }
    }

    private async Task DoSubmit()
    {
        if (_qty <= 0 || _price <= 0)
        {
            _err = "Quantity and price must be positive.";
            return;
        }
        var input = new LogTradeInput(
            DateOnly.FromDateTime(_date),
            Enum.Parse<TradeSide>(_side),
            _qty, _price, _fees, _note);

        await OnSubmit.InvokeAsync(input);
    }
}
