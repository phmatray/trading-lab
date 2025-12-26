using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for deleting historical data.
/// Provides simple deletion operations with clear result messages.
/// </summary>
public class DeleteHistoricalDataUseCase : IDeleteHistoricalDataUseCase
{
    private readonly IHistoricalDataPort _historicalDataPort;

    public DeleteHistoricalDataUseCase(IHistoricalDataPort historicalDataPort)
    {
        _historicalDataPort = historicalDataPort;
    }

    public async Task<DeleteDataResult> DeleteTickerAsync(string ticker, TimeFrame? timeFrame = null)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            throw new ArgumentException("Ticker cannot be null or empty.", nameof(ticker));
        }

        int recordsDeleted = await _historicalDataPort.DeleteTickerDataAsync(ticker, timeFrame);

        string message = timeFrame != null
            ? $"Deleted {recordsDeleted} record(s) for {ticker} ({timeFrame.Unit})."
            : $"Deleted {recordsDeleted} record(s) for {ticker} (all timeframes).";

        return new DeleteDataResult(recordsDeleted, message);
    }

    public async Task<DeleteDataResult> DeleteDateRangeAsync(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            throw new ArgumentException("Ticker cannot be null or empty.", nameof(ticker));
        }

        if (startDate > endDate)
        {
            throw new ArgumentException("Start date must be less than or equal to end date.");
        }

        int recordsDeleted = await _historicalDataPort.DeleteDateRangeAsync(
            ticker,
            timeFrame,
            startDate,
            endDate);

        string message = $"Deleted {recordsDeleted} record(s) for {ticker} ({timeFrame.Unit}) " +
                        $"between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}.";

        return new DeleteDataResult(recordsDeleted, message);
    }
}
