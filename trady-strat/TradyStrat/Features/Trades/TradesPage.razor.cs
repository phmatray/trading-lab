using TradyStrat.Application.Portfolio;
using TradyStrat.Application.Trades.UseCases;
using Ardalis.Specification;
using Microsoft.AspNetCore.Components;
using TradyStrat.Application.UseCases;
using TradyStrat.Application.Settings;
using TradyStrat.Application.Settings.UseCases;
using TradyStrat.Domain;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared.Market;

namespace TradyStrat.Features.Trades;

public partial class TradesPage : ComponentBase
{
    [Inject] private IPortfolioRepository Portfolios { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private IFocusTickerRepository FocusTickerRepo { get; set; } = default!;
    [Inject] private LogTradeUseCase LogTrade { get; set; } = default!;
    [Inject] private DeleteTradeUseCase DeleteTrade { get; set; } = default!;
    [Inject] private ImportTradesCsvUseCase ImportCsv { get; set; } = default!;
    [Inject] private ListInstrumentsUseCase ListInstruments { get; set; } = default!;

    private string _focusTicker = "";

    private List<Trade> _trades = new();
    // Cumulative net cash flow per row (in EUR) for the trade ledger UI.
    private List<(Trade Trade, decimal CumulativeEur, int InstrumentId)> _rows = new();
    private Dictionary<int, string> _instrumentTickers = new();
    private int _count;
    private bool _showAdd;
    private bool _showImport;
    private Trade? _pendingDelete;
    private int _pendingDeleteInstrumentId;
    private string _csvText = "";
    private string? _importError;

    protected override async Task OnInitializedAsync()
    {
        _focusTicker = (await FocusTickerRepo.GetAsync(CancellationToken.None)).Value;
        await Reload();
    }

    private async Task Reload()
    {
        var portfolio = await Portfolios.GetAsync(CancellationToken.None);

        // Flatten trades across positions, retaining the originating InstrumentId.
        var flat = portfolio.Positions
            .SelectMany(p => p.Trades.Select(t => (Trade: t, InstrumentId: p.InstrumentId.Value)))
            .OrderBy(x => x.Trade.ExecutedOn)
            .ThenBy(x => x.Trade.Id.Value)
            .ToList();

        _trades = flat.Select(x => x.Trade).ToList();
        _count = _trades.Count;

        var running = 0m;
        _rows = flat.Select(x =>
        {
            var net = x.Trade.Net.Amount;
            running += x.Trade.IsBuy ? net : -net;
            return (x.Trade, running, x.InstrumentId);
        }).ToList();

        var instruments = await ListInstruments.ExecuteAsync(Unit.Value, CancellationToken.None);
        _instrumentTickers = instruments.ToDictionary(i => i.Id.Value, i => i.Ticker);
    }

    private string TickerFor(int instrumentId) =>
        _instrumentTickers.TryGetValue(instrumentId, out var ticker) ? ticker : "—";

    private void OnAddClicked()
    {
        _showAdd = true;
    }

    private void OnImportClicked()
    {
        _csvText = "";
        _importError = null;
        _showImport = true;
    }

    private async Task HandleSubmit(LogTradeInput input)
    {
        await LogTrade.ExecuteAsync(input, CancellationToken.None);
        CloseDialogs();
        await Reload();
    }

    private async Task DeleteAsync(Trade t)
    {
        await DeleteTrade.ExecuteAsync(new DeleteTradeInput(t.Id.Value), CancellationToken.None);
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
        _importError = null;
        _pendingDelete = null;
    }
}
