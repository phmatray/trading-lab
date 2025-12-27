using TradingStrat.Domain.Common;

namespace TradingStrat.Application.UseCases;

/// <summary>
/// Base class for use cases with IProgress parameters that eliminates try-catch boilerplate.
/// Uses the Template Method pattern to provide consistent error handling and Result wrapping.
/// </summary>
/// <typeparam name="TCommand">The command type for the use case.</typeparam>
/// <typeparam name="TResult">The result type returned on success.</typeparam>
public abstract class BaseProgressUseCase<TCommand, TResult>
{
    /// <summary>
    /// Template method that wraps execution in try-catch and returns Result&lt;TResult&gt;.
    /// Derived classes provide business logic via executeCore delegate.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="progress">Optional progress reporter for status updates.</param>
    /// <param name="executeCore">The business logic to execute (provided by derived class).</param>
    /// <param name="errorCode">The error code to use for generic failures.</param>
    /// <returns>Result containing the success value or errors.</returns>
    protected async Task<Result<TResult>> ExecuteAsync(
        TCommand command,
        IProgress<string>? progress,
        Func<TCommand, IProgress<string>?, Task<TResult>> executeCore,
        string errorCode)
    {
        try
        {
            TResult result = await executeCore(command, progress);
            return Result<TResult>.Success(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return Result<TResult>.Failure(
                Error.NotFound(ex.Message, $"{errorCode}_NOT_FOUND"));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return Result<TResult>.Failure(
                Error.Conflict(ex.Message, $"{errorCode}_CONFLICT"));
        }
        catch (ArgumentException ex)
        {
            return Result<TResult>.Failure(
                Error.Validation(ex.Message, $"{errorCode}_VALIDATION"));
        }
        catch (Exception ex)
        {
            return Result<TResult>.Failure(
                Error.BusinessRule($"Failed: {ex.Message}", $"{errorCode}_FAILED"));
        }
    }
}
