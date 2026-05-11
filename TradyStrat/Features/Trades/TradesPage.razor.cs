using System.Globalization;
using Ardalis.Specification;
using Microsoft.AspNetCore.Components;
using TradyStrat.Common.UseCases;
using TradyStrat.Features.Settings.Config;
using TradyStrat.Features.Settings.UseCases;
using TradyStrat.Features.Trades.UseCases;
using TradyStrat.Common.Domain;
using TradyStrat.Common.Exceptions;
using TradyStrat.Common.Time;
using TradyStrat.Features.Trades.Specifications;

namespace TradyStrat.Features.Trades;

public partial class TradesPage : ComponentBase
{
    [Inject] private IReadRepositoryBase<Trade> Repo { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private ISettingsReader Settings { get; set; } = default!;
    [Inject] private LogTradeUseCase LogTrade { get; set; } = default!;
    [Inject] private EditTradeUseCase EditTrade { get; set; } = default!;
    [Inject] private DeleteTradeUseCase DeleteTrade { get; set; } = default!;
    [Inject] private ImportTradesCsvUseCase ImportCsv { get; set; } = default!;
    [Inject] private ListInstrumentsUseCase ListInstruments { get; set; } = default!;

    private string _focusTicker = "";
    private static readonly CultureInfo FrFr = CultureInfo.GetCultureInfo("fr-FR");

    // Show share counts compactly: integers as N0, fractional shares with up
    // to 4 decimals but trailing zeros trimmed. So 42100,0000 → 42 100, but
    // 12,5000 → 12,5 — no decimal noise on integer trades.
    private static string FormatQty(decimal q)
    {
        if (q == decimal.Truncate(q)) return q.ToString("N0", FrFr);
        var s = q.ToString("N4", FrFr);
        // Trim trailing zeros after the decimal separator, then a hanging separator.
        var sep = FrFr.NumberFormat.NumberDecimalSeparator;
        var idx = s.IndexOf(sep, StringComparison.Ordinal);
        if (idx < 0) return s;
        s = s.TrimEnd('0');
        if (s.EndsWith(sep, StringComparison.Ordinal)) s = s[..^sep.Length];
        return s;
    }

    private List<Trade> _trades = new();
    private Dictionary<int, string> _instrumentTickers = new();
    private int _count;
    private bool _showAdd;
    private bool _showImport;
    private Trade? _editing;
    private string _csvText = "";
    private string? _importError;

    protected override async Task OnInitializedAsync()
    {
        _focusTicker = await Settings.FocusTickerAsync(CancellationToken.None);
        await Reload();
    }

    private async Task Reload()
    {
        var list = await Repo.ListAsync(new AllTradesSpec(), CancellationToken.None);
        _trades = list.ToList();
        _count = _trades.Count;

        var instruments = await ListInstruments.ExecuteAsync(Unit.Value, CancellationToken.None);
        _instrumentTickers = instruments.ToDictionary(i => i.Id, i => i.Ticker);
    }

    private string TickerFor(int instrumentId) =>
        _instrumentTickers.TryGetValue(instrumentId, out var ticker) ? ticker : "—";

    private void OnAddClicked()
    {
        _editing = null;
        _showAdd = true;
    }

    private void OnImportClicked()
    {
        _csvText = "";
        _importError = null;
        _showImport = true;
    }

    private void StartEdit(Trade t)
    {
        _editing = t;
        _showAdd = true;
    }

    private async Task HandleSubmit(LogTradeInput input)
    {
        if (_editing is null)
        {
            await LogTrade.ExecuteAsync(input, CancellationToken.None);
        }
        else
        {
            await EditTrade.ExecuteAsync(new EditTradeInput(
                _editing.Id, input.InstrumentId, input.ExecutedOn, input.Side,
                input.Quantity, input.PricePerShare, input.FeesEur, input.Note), CancellationToken.None);
        }
        CloseDialogs();
        await Reload();
    }

    private async Task DeleteAsync(Trade t)
    {
        await DeleteTrade.ExecuteAsync(new DeleteTradeInput(t.Id), CancellationToken.None);
        await Reload();
    }

    private async Task DoImport()
    {
        try
        {
            await ImportCsv.ExecuteAsync(new ImportTradesCsvInput(_csvText), CancellationToken.None);
            CloseDialogs();
            await Reload();
        }
        catch (CsvImportException ex)
        {
            _importError = ex.Message;
        }
    }

    private void CloseDialogs()
    {
        _showAdd = false;
        _showImport = false;
        _editing = null;
        _importError = null;
    }
}
