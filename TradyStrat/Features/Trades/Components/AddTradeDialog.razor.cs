using Ardalis.Specification;
using Microsoft.AspNetCore.Components;
using TradyStrat.Common.Domain;
using TradyStrat.Features.Settings.Specifications;
using TradyStrat.Features.Trades.UseCases;

namespace TradyStrat.Features.Trades.Components;

public partial class AddTradeDialog : ComponentBase
{
    // The instrument-picker dropdown lands in Task 15. Until then the dialog
    // resolves the focus instrument (CON3.L) via repository lookup so trades
    // log with a valid InstrumentId after the schema widening in Task 9.
    private const string FocusTicker = "CON3.L";

    [Inject] private IReadRepositoryBase<Instrument> Instruments { get; set; } = default!;

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
    private int? _instrumentId;

    protected override async Task OnInitializedAsync()
    {
        if (Initial is { } t)
        {
            _date  = t.ExecutedOn.ToDateTime(TimeOnly.MinValue);
            _side  = t.Side.ToString();
            _qty   = t.Quantity;
            _price = t.PricePerShare;
            _fees  = t.FeesEur;
            _note  = t.Note;
            _instrumentId = t.InstrumentId;
        }
        else
        {
            var focus = await Instruments.FirstOrDefaultAsync(
                new InstrumentByTickerSpec(FocusTicker), CancellationToken.None);
            _instrumentId = focus?.Id;
        }
    }

    private async Task DoSubmit()
    {
        if (_qty <= 0 || _price <= 0)
        {
            _err = "Quantity and price must be positive.";
            return;
        }
        if (_instrumentId is null)
        {
            _err = $"Focus instrument '{FocusTicker}' is not registered. Add it from Settings first.";
            return;
        }
        var input = new LogTradeInput(
            _instrumentId.Value,
            DateOnly.FromDateTime(_date),
            Enum.Parse<TradeSide>(_side),
            _qty, _price, _fees, _note);

        await OnSubmit.InvokeAsync(input);
    }
}
