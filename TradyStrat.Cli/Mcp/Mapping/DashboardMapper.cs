using TradyStrat.Application.Dashboard;
using TradyStrat.Cli.Mcp.Dto;
using TradyStrat.Domain;

namespace TradyStrat.Cli.Mcp.Mapping;

/// <summary>
/// Maps <see cref="DashboardViewModel"/> + <see cref="IndicatorReading"/> to
/// <see cref="DashboardSnapshot"/>.
///
/// The <paramref name="reading"/> parameter is required for the indicators block
/// and per-indicator zone breakdown because those scalars live in IndicatorReading,
/// not in DashboardViewModel. The overall Zone on all four ByIndicator slots is
/// intentionally the same value (the engine's aggregate zone) — per-indicator zone
/// decomposition is not surfaced by the current IndicatorReading shape.
/// </summary>
internal static class DashboardMapper
{
    public static DashboardSnapshot ToSnapshot(DashboardViewModel vm, IndicatorReading reading)
    {
        var focusView = vm.Tickers.FirstOrDefault(t => t.Ticker == vm.FocusTicker);

        // Last close: USD price from the focus TickerView (raw price field),
        // EUR price from PriceEur. FX rate computed as Usd/Eur when both present.
        var usd = focusView?.Price ?? reading.Price;
        var eur = focusView?.PriceEur ?? reading.Price;
        var fxRate = eur != 0m ? Math.Round(usd / eur, 6) : 1m;

        var zone = reading.Zone.ToString();
        var zoneBlock = new ZoneBlock(
            Overall: zone,
            ByIndicator: new ZoneByIndicator(
                Bollinger: zone,
                Rsi: zone,
                Sma: zone,
                Ichimoku: zone));

        var indicators = new IndicatorsBlock(
            Bollinger: new BollingerBlock(
                Upper: reading.Bollinger?.Upper,
                Mid: reading.Bollinger?.Middle,
                Lower: reading.Bollinger?.Lower),
            Rsi: new RsiBlock(Value: reading.Rsi),
            Sma: new SmaBlock(Sma50: reading.Sma50, Sma200: reading.Sma200),
            Ichimoku: new IchimokuBlock(
                Tenkan: reading.Ichimoku?.Tenkan,
                Kijun: reading.Ichimoku?.Kijun,
                SenkouA: reading.Ichimoku?.SenkouA,
                SenkouB: reading.Ichimoku?.SenkouB,
                Chikou: reading.Ichimoku?.Chikou));

        // Surface a suggestion only when the focus is in the Ready state. Pending
        // and Failed states do not have an underlying Suggestion row to map.
        var focusSuggestion = vm.FocusCallState is SuggestionState.Ready ready ? ready.Suggestion : null;
        var suggestion = focusSuggestion is null ? null : new SuggestionBrief(
            Date: focusSuggestion.ForDate,
            Action: focusSuggestion.Action.ToString(),
            Conviction: focusSuggestion.Conviction,
            Reasoning: focusSuggestion.Rationale,
            EnvelopeHash: Truncate(focusSuggestion.EnvelopeHash),
            PromptVersionHash: Truncate(focusSuggestion.PromptVersionHash));

        var position = BuildPosition(vm);

        return new DashboardSnapshot(
            Ticker: vm.FocusTicker,
            AsOfDate: vm.Today,
            LastClose: new MoneyDualCurrency(Usd: usd, Eur: eur, FxRate: fxRate),
            Zone: zoneBlock,
            Indicators: indicators,
            Suggestion: suggestion,
            Position: position);
    }

    private static PositionBrief BuildPosition(DashboardViewModel vm)
    {
        var row = vm.Positions.FirstOrDefault(p => p.Ticker == vm.FocusTicker);
        return row is null
            ? new PositionBrief(0m, 0m, 0m, 0m, 0m)
            : new PositionBrief(
                Qty: row.Quantity.Value,
                CostBasisEur: row.CostBasisEur.Amount,
                MarketValueEur: row.MarketValueEur.Amount,
                UnrealizedPnlEur: row.UnrealizedPnLEur.Amount,
                RealizedPnlEur: row.RealizedPnLEur.Amount);
    }

    private static string? Truncate(string? h)
        => h is null ? null : (h.Length >= 8 ? h[..8] : h);
}
