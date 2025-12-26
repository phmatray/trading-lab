using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TradingStrat.Application.Services;
using TradingStrat.Domain.ValueObjects;
using TradingStrat.Infrastructure.Configuration;

namespace TradingStrat.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that automatically refreshes stale historical data on a schedule.
/// Runs daily at a configurable time and refreshes all tickers with outdated data.
/// </summary>
public class DataRefreshBackgroundService : BackgroundService
{
    private readonly IDataRefreshService _dataRefreshService;
    private readonly ILogger<DataRefreshBackgroundService> _logger;
    private readonly DataRefreshConfiguration _config;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15); // Check every 15 minutes

    public DataRefreshBackgroundService(
        IDataRefreshService dataRefreshService,
        IOptions<DataRefreshConfiguration> config,
        ILogger<DataRefreshBackgroundService> logger)
    {
        _dataRefreshService = dataRefreshService;
        _config = config.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Data refresh background service is disabled");
            return;
        }

        _logger.LogInformation(
            "Data refresh background service started. Schedule hour: {ScheduleHour}, Stale threshold: {StaleThresholdHours}h",
            _config.ScheduleHour,
            _config.StaleThresholdHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DateTime now = DateTime.Now;
                DateTime nextRun = GetNextRunTime(now);

                TimeSpan delay = nextRun - now;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next data refresh scheduled for {NextRun}", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await RefreshDataAsync(stoppingToken);
                }

                // Wait at least the check interval before next iteration
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Data refresh background service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in data refresh background service");
                // Wait before retrying
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task RefreshDataAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting scheduled data refresh");

        foreach (string timeFrameStr in _config.TimeFrames)
        {
            try
            {
                TimeFrame timeFrame = ParseTimeFrame(timeFrameStr);

                _logger.LogInformation("Refreshing stale data for timeframe: {TimeFrame}", timeFrame.Unit);

                Progress<RefreshProgress> progress = new(p =>
                {
                    if (!string.IsNullOrEmpty(p.CurrentTicker))
                    {
                        _logger.LogInformation(
                            "Refreshing {CurrentTicker} ({Completed}/{Total}) - Success: {Success}, Failed: {Failed}",
                            p.CurrentTicker,
                            p.CompletedTickers,
                            p.TotalTickers,
                            p.SuccessfulTickers,
                            p.FailedTickers);
                    }
                });

                RefreshResult result = await _dataRefreshService.RefreshAllStaleDataAsync(
                    timeFrame,
                    _config.StaleThresholdHours,
                    progress,
                    cancellationToken);

                _logger.LogInformation(
                    "Data refresh completed for {TimeFrame}. Processed: {Processed}, Successful: {Successful}, Failed: {Failed}, Skipped: {Skipped}",
                    result.TimeFrame.Unit,
                    result.TotalTickersProcessed,
                    result.SuccessfulRefreshes,
                    result.FailedRefreshes,
                    result.SkippedTickers);

                if (result.Failures.Any())
                {
                    _logger.LogWarning(
                        "Failed to refresh {FailureCount} tickers: {FailedTickers}",
                        result.Failures.Count,
                        string.Join(", ", result.Failures.Keys));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing data for timeframe: {TimeFrame}", timeFrameStr);
            }
        }

        _logger.LogInformation("Scheduled data refresh completed");
    }

    private DateTime GetNextRunTime(DateTime currentTime)
    {
        DateTime today = currentTime.Date;
        DateTime scheduledTime = today.AddHours(_config.ScheduleHour);

        if (currentTime >= scheduledTime)
        {
            // Already past today's scheduled time, schedule for tomorrow
            return scheduledTime.AddDays(1);
        }

        return scheduledTime;
    }

    private TimeFrame ParseTimeFrame(string timeFrameStr)
    {
        return timeFrameStr.ToUpperInvariant() switch
        {
            "M1" => new TimeFrame { Unit = TimeFrameUnit.M1 },
            "M5" => new TimeFrame { Unit = TimeFrameUnit.M5 },
            "M15" => new TimeFrame { Unit = TimeFrameUnit.M15 },
            "M30" => new TimeFrame { Unit = TimeFrameUnit.M30 },
            "H1" => new TimeFrame { Unit = TimeFrameUnit.H1 },
            "H4" => new TimeFrame { Unit = TimeFrameUnit.H4 },
            "D1" => new TimeFrame { Unit = TimeFrameUnit.D1 },
            "W1" => new TimeFrame { Unit = TimeFrameUnit.W1 },
            "MN1" => new TimeFrame { Unit = TimeFrameUnit.MN1 },
            _ => new TimeFrame { Unit = TimeFrameUnit.D1 }
        };
    }
}
