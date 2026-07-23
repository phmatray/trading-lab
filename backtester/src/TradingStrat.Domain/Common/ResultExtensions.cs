namespace TradingStrat.Domain.Common;

/// <summary>
/// Extension methods for working with Result pattern.
/// Provides functional-style operations for chaining and transforming results.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Transforms the value of a successful result using the provided function.
    /// If the result is a failure, returns a new failure with the same errors.
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, TOut> mapFunc)
    {
        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors);
        }

        return Result<TOut>.Success(mapFunc(result.Value));
    }

    /// <summary>
    /// Transforms the value of a successful result using an async function.
    /// </summary>
    public static async Task<Result<TOut>> MapAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> mapFunc)
    {
        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors);
        }

        TOut value = await mapFunc(result.Value);
        return Result<TOut>.Success(value);
    }

    /// <summary>
    /// Binds (flatMaps) a result to another result-returning function.
    /// Prevents nested Result&lt;Result&lt;T&gt;&gt; scenarios.
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Result<TOut>> bindFunc)
    {
        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors);
        }

        return bindFunc(result.Value);
    }

    /// <summary>
    /// Async version of Bind for result-returning async functions.
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> bindFunc)
    {
        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors);
        }

        return await bindFunc(result.Value);
    }

    /// <summary>
    /// Async version of Bind for Task&lt;Result&lt;T&gt;&gt; inputs.
    /// </summary>
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(
        this Task<Result<TIn>> resultTask,
        Func<TIn, Task<Result<TOut>>> bindFunc)
    {
        Result<TIn> result = await resultTask;

        if (result.IsFailure)
        {
            return Result<TOut>.Failure(result.Errors);
        }

        return await bindFunc(result.Value);
    }

    /// <summary>
    /// Executes an action if the result is successful, returns the original result.
    /// Useful for side effects like logging without breaking the chain.
    /// </summary>
    public static Result<T> Tap<T>(
        this Result<T> result,
        Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Async version of Tap.
    /// </summary>
    public static async Task<Result<T>> TapAsync<T>(
        this Result<T> result,
        Func<T, Task> action)
    {
        if (result.IsSuccess)
        {
            await action(result.Value);
        }

        return result;
    }

    /// <summary>
    /// Executes an action if the result is a failure, returns the original result.
    /// Useful for error logging without breaking the chain.
    /// </summary>
    public static Result<T> TapError<T>(
        this Result<T> result,
        Action<List<Error>> action)
    {
        if (result.IsFailure)
        {
            action(result.Errors);
        }

        return result;
    }

    /// <summary>
    /// Provides a fallback value if the result is a failure.
    /// </summary>
    public static T Match<T>(
        this Result<T> result,
        Func<T, T> onSuccess,
        Func<List<Error>, T> onFailure)
    {
        return result.IsSuccess
            ? onSuccess(result.Value)
            : onFailure(result.Errors);
    }

    /// <summary>
    /// Gets the value if successful, or the provided default value if failed.
    /// </summary>
    public static T GetValueOrDefault<T>(
        this Result<T> result,
        T defaultValue)
    {
        return result.IsSuccess ? result.Value : defaultValue;
    }

    /// <summary>
    /// Gets the value if successful, or computes a default using the provided function.
    /// </summary>
    public static T GetValueOrDefault<T>(
        this Result<T> result,
        Func<List<Error>, T> defaultFunc)
    {
        return result.IsSuccess ? result.Value : defaultFunc(result.Errors);
    }

    /// <summary>
    /// Combines multiple results into a single result.
    /// If any result is a failure, returns a failure with all errors.
    /// </summary>
    public static Result<List<T>> Combine<T>(params Result<T>[] results)
    {
        List<Error> allErrors = new();

        foreach (Result<T> result in results)
        {
            if (result.IsFailure)
            {
                allErrors.AddRange(result.Errors);
            }
        }

        if (allErrors.Any())
        {
            return Result<List<T>>.Failure(allErrors);
        }

        List<T> values = results.Select(r => r.Value).ToList();
        return Result<List<T>>.Success(values);
    }
}
