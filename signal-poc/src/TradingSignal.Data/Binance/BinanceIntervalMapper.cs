using Binance.Net.Enums;

namespace TradingSignal.Data.Binance;

internal static class BinanceIntervalMapper
{
    public static KlineInterval ToKlineInterval(TimeSpan interval) => interval switch
    {
        _ when interval == TimeSpan.FromMinutes(1) => KlineInterval.OneMinute,
        _ when interval == TimeSpan.FromMinutes(3) => KlineInterval.ThreeMinutes,
        _ when interval == TimeSpan.FromMinutes(5) => KlineInterval.FiveMinutes,
        _ when interval == TimeSpan.FromMinutes(15) => KlineInterval.FifteenMinutes,
        _ when interval == TimeSpan.FromMinutes(30) => KlineInterval.ThirtyMinutes,
        _ when interval == TimeSpan.FromHours(1) => KlineInterval.OneHour,
        _ when interval == TimeSpan.FromHours(2) => KlineInterval.TwoHour,
        _ when interval == TimeSpan.FromHours(4) => KlineInterval.FourHour,
        _ when interval == TimeSpan.FromHours(6) => KlineInterval.SixHour,
        _ when interval == TimeSpan.FromHours(8) => KlineInterval.EightHour,
        _ when interval == TimeSpan.FromHours(12) => KlineInterval.TwelveHour,
        _ when interval == TimeSpan.FromDays(1) => KlineInterval.OneDay,
        _ when interval == TimeSpan.FromDays(3) => KlineInterval.ThreeDay,
        _ when interval == TimeSpan.FromDays(7) => KlineInterval.OneWeek,
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Unsupported kline interval"),
    };
}
