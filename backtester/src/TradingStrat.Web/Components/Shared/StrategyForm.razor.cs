using Microsoft.AspNetCore.Components;

namespace TradingStrat.Web.Components.Shared;

public partial class StrategyForm : ComponentBase
{
    [Parameter] public string SelectedStrategy { get; set; } = "ma";
    [Parameter] public EventCallback<string> SelectedStrategyChanged { get; set; }
    [Parameter] public EventCallback<Dictionary<string, object>> ParametersChanged { get; set; }

    // MA parameters
    private int FastPeriod { get; set; } = 20;
    private int SlowPeriod { get; set; } = 50;

    // RSI parameters
    private int RSIPeriod { get; set; } = 14;
    private decimal OversoldLevel { get; set; } = 30;
    private decimal OverboughtLevel { get; set; } = 70;

    // MACD parameters
    private int MACDFast { get; set; } = 12;
    private int MACDSlow { get; set; } = 26;
    private int MACDSignal { get; set; } = 9;

    // ML parameters
    private decimal BuyThreshold { get; set; } = 1.0m;
    private decimal SellThreshold { get; set; } = -1.0m;

    // Ichimoku parameters
    private int TenkanPeriod { get; set; } = 9;
    private int KijunPeriod { get; set; } = 26;
    private int SenkouBPeriod { get; set; } = 52;
    private int Displacement { get; set; } = 26;
    private string IchimokuExitMode { get; set; } = "CloseBelowKijun";
    private string IchimokuEntryMode { get; set; } = "AllConditionsOnly";
    private int CrossLookbackDays { get; set; } = 5;
    private decimal RiskPercentage { get; set; } = 2.0m;

    private async Task OnStrategyChanged()
    {
        await SelectedStrategyChanged.InvokeAsync(SelectedStrategy);
        await NotifyParametersChanged();
    }

    private async Task NotifyParametersChanged()
    {
        Dictionary<string, object> parameters = GetCurrentParameters();
        await ParametersChanged.InvokeAsync(parameters);
    }

    public Dictionary<string, object> GetCurrentParameters()
    {
        return SelectedStrategy switch
        {
            "ma" => new Dictionary<string, object>
            {
                ["FastPeriod"] = FastPeriod,
                ["SlowPeriod"] = SlowPeriod
            },
            "rsi" => new Dictionary<string, object>
            {
                ["Period"] = RSIPeriod,
                ["OversoldThreshold"] = OversoldLevel,
                ["OverboughtThreshold"] = OverboughtLevel
            },
            "macd" => new Dictionary<string, object>
            {
                ["FastPeriod"] = MACDFast,
                ["SlowPeriod"] = MACDSlow,
                ["SignalPeriod"] = MACDSignal
            },
            "ml" => new Dictionary<string, object>
            {
                ["BuyThreshold"] = BuyThreshold / 100m,
                ["SellThreshold"] = SellThreshold / 100m
            },
            "ichimoku" => new Dictionary<string, object>
            {
                ["TenkanPeriod"] = TenkanPeriod,
                ["KijunPeriod"] = KijunPeriod,
                ["SenkouBPeriod"] = SenkouBPeriod,
                ["Displacement"] = Displacement,
                ["ExitMode"] = IchimokuExitMode,
                ["EntryMode"] = IchimokuEntryMode,
                ["CrossLookbackDays"] = CrossLookbackDays,
                ["RiskPercentage"] = RiskPercentage / 100m
            },
            _ => new Dictionary<string, object>()
        };
    }
}
