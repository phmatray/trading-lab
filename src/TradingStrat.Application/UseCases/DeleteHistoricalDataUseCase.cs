using TradingStrat.Application.Common;
using TradingStrat.Application.Ports.Inbound;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Common;
using TradingStrat.Domain.ValueObjects;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Use case for deleting historical data.
/// Provides simple deletion operations with clear result messages.
/// Uses helper method pattern to eliminate try-catch boilerplate.
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
        // Explicit validation to preserve specific error codes
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return Result<DeleteDataResult>.Failure(
                Error.Validation("Ticker cannot be null or empty.", ErrorCodes.Data.TickerRequired));
        }

        return await ExecuteWithErrorHandling(() => DeleteTickerCoreAsync(ticker, timeFrame), ErrorCodes.Data.DeleteFailed);
    }

    public async Task<Result<DeleteDataResult>> DeleteDateRangeAsync(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate)
    {
        // Explicit validation to preserve specific error codes
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return Result<DeleteDataResult>.Failure(
                Error.Validation("Ticker cannot be null or empty.", ErrorCodes.Data.TickerRequired));
        }

        if (startDate > endDate)
        {
            return Result<DeleteDataResult>.Failure(
                Error.Validation("Start date must be less than or equal to end date.", ErrorCodes.Data.InvalidDateRange));
        }

        return await ExecuteWithErrorHandling(() => DeleteDateRangeCoreAsync(ticker, timeFrame, startDate, endDate), ErrorCodes.Data.DeleteFailed);
    }

    private static async Task<Result<T>> ExecuteWithErrorHandling<T>(
        Func<Task<T>> executeCore,
        string errorCode)
    {
        try
        {
            T result = await executeCore();
            return Result<T>.Success(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Result<T>.Failure(
                Error.NotFound(ex.Message, $"{errorCode}_NOT_FOUND"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Result<T>.Failure(
                Error.Conflict(ex.Message, $"{errorCode}_CONFLICT"));
        }
        catch (ArgumentException ex)
        {
            return Result<T>.Failure(
                Error.Validation(ex.Message, $"{errorCode}_VALIDATION"));
        }
        catch (Exception ex)
        {
            return Result<T>.Failure(
                Error.BusinessRule($"Failed: {ex.Message}", $"{errorCode}_FAILED"));
        }
    }

    private async Task<DeleteDataResult> DeleteTickerCoreAsync(string ticker, TimeFrame? timeFrame)
    {
        int recordsDeleted = await _historicalDataPort.DeleteTickerDataAsync(ticker, timeFrame);

        string message = timeFrame != null
            ? $"Deleted {recordsDeleted} record(s) for {ticker} ({timeFrame.Unit})."
            : $"Deleted {recordsDeleted} record(s) for {ticker} (all timeframes).";

        return new DeleteDataResult(recordsDeleted, message);
    }

    private async Task<DeleteDataResult> DeleteDateRangeCoreAsync(
        string ticker,
        TimeFrame timeFrame,
        DateTime startDate,
        DateTime endDate)
    {
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
