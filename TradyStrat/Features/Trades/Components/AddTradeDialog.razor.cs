using Microsoft.AspNetCore.Components;
using TradyStrat.Domain;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Settings.UseCases;
using TradyStrat.Features.Trades.UseCases;

namespace TradyStrat.Features.Trades.Components;

public partial class AddTradeDialog : ComponentBase
{
    [Inject] private ListInstrumentsUseCase ListInstruments { get; set; } = default!;

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
    private int _instrumentId;
    private List<Instrument> _heldInstruments = new();

    protected override async Task OnInitializedAsync()
    {
        var all = await ListInstruments.ExecuteAsync(Unit.Value, CancellationToken.None);
        _heldInstruments = all.Where(i => i.Kind == InstrumentKind.Held).ToList();

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
        else if (_heldInstruments.Count == 1)
        {
            _instrumentId = _heldInstruments[0].Id;
        }
    }

    private async Task DoSubmit()
    {
        if (_instrumentId == 0)
        {
            _err = "Pick an instrument.";
            return;
        }
        if (_qty <= 0 || _price <= 0)
        {
            _err = "Quantity and price must be positive.";
            return;
        }
        var input = new LogTradeInput(
            _instrumentId,
            DateOnly.FromDateTime(_date),
            Enum.Parse<TradeSide>(_side),
            _qty, _price, _fees, _note);

        await OnSubmit.InvokeAsync(input);
    }
}
