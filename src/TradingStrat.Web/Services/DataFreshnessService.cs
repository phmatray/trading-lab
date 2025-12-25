using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Web.Models;

namespace TradingStrat.Web.Services;

/// <summary>
/// Service for checking data freshness and notifying users when data is stale.
/// </summary>
public class DataFreshnessService : IDataFreshnessService
{
    private readonly IHistoricalDataPort _historicalDataPort;
    private readonly NotificationService _notificationService;

    public DataFreshnessService(
        IHistoricalDataPort historicalDataPort,
        NotificationService notificationService)
    {
        _historicalDataPort = historicalDataPort;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Checks if data for the specified ticker is stale and sends a notification if needed.
    /// Data is considered stale if it's more than 1 day old.
    /// Sends a warning notification if data is more than 7 days old, otherwise sends an info notification.
    /// </summary>
    public async Task CheckAndNotifyAsync(string ticker, CancellationToken cancellationToken = default)
    {
        // Check D1 (daily) data freshness by default
        DateTime? latestDate = await _historicalDataPort.GetLatestDataDateAsync(ticker, TimeFrame.D1);

        if (!latestDate.HasValue)
        {
            return;
        }

        int daysOld = (DateTime.Today - latestDate.Value.Date).Days;

        if (daysOld > 1)
        {
            NotificationSeverity severity = daysOld > 7
                ? NotificationSeverity.Warning
                : NotificationSeverity.Info;

            await _notificationService.AddNotificationAsync(
                NotificationType.DataFreshness,
                severity,
                "Data Refresh Recommended",
                $"Data for {ticker} is {daysOld} day{(daysOld == 1 ? "" : "s")} old",
                ticker: ticker,
                action: new NotificationAction
                {
                    Label = "Refresh Now",
                    TargetPage = "/data"
                }
            );
        }
    }
}
