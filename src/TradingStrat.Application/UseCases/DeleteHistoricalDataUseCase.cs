using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
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

    public async Task<Result<DeleteDataResult>> DeleteTickerAsync(string ticker, TimeFrame? timeFrame = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return Result<DeleteDataResult>.Failure(
                    Error.Validation("Ticker cannot be null or empty.", "TICKER_REQUIRED"));
            }

            int recordsDeleted = await _historicalDataPort.DeleteTickerDataAsync(ticker, timeFrame);

            string message = timeFrame != null
                ? $"Deleted {recordsDeleted} record(s) for {ticker} ({timeFrame.Unit})."
                : $"Deleted {recordsDeleted} record(s) for {ticker} (all timeframes).";

            return Result<DeleteDataResult>.Success(new DeleteDataResult(recordsDeleted, message));
        }
        catch (Exception ex)
        {
            return Result<DeleteDataResult>.Failure(
                Error.BusinessRule($"Failed to delete data for {ticker}: {ex.Message}", "DELETE_DATA_FAILED"));
        }
    }

    public async Task<Result<DeleteDataResult>> DeleteDateRangeAsync(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                return Result<DeleteDataResult>.Failure(
                    Error.Validation("Ticker cannot be null or empty.", "TICKER_REQUIRED"));
            }

            if (startDate > endDate)
            {
                return Result<DeleteDataResult>.Failure(
                    Error.Validation("Start date must be less than or equal to end date.", "INVALID_DATE_RANGE"));
            }

            int recordsDeleted = await _historicalDataPort.DeleteDateRangeAsync(
                ticker,
                timeFrame,
                startDate,
                endDate);

            string message = $"Deleted {recordsDeleted} record(s) for {ticker} ({timeFrame.Unit}) " +
                            $"between {startDate:yyyy-MM-dd} and {endDate:yyyy-MM-dd}.";

            return Result<DeleteDataResult>.Success(new DeleteDataResult(recordsDeleted, message));
        }
        catch (Exception ex)
        {
            return Result<DeleteDataResult>.Failure(
                Error.BusinessRule($"Failed to delete data for {ticker}: {ex.Message}", "DELETE_DATA_FAILED"));
        }
    }
}
