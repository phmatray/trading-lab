namespace TradingStrat.Domain.Common;

/// <summary>
/// Represents the result of an operation that can either succeed with a value or fail with errors.
/// Provides type-safe error handling without exceptions for domain operations.
/// </summary>
/// <typeparam name="T">The type of the value returned on success.</typeparam>
public sealed class Result<T>
{
    private T? _value;

    private Result() { }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; private init; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the list of errors that occurred during the operation.
    /// Empty if the operation succeeded.
    /// </summary>
    public List<Error> Errors { get; private init; } = new();

    /// <summary>
    /// Gets the value returned by the operation.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing value on a failed result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on failed result");

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value to return.</param>
    /// <returns>A successful result containing the value.</returns>
    public static Result<T> Success(T value)
        => new() { IsSuccess = true, _value = value };

    /// <summary>
    /// Creates a failed result with the specified errors.
    /// </summary>
    /// <param name="errors">The errors that occurred.</param>
    /// <returns>A failed result containing the errors.</returns>
    public static Result<T> Failure(params Error[] errors)
        => new() { IsSuccess = false, Errors = errors.ToList() };

    /// <summary>
    /// Creates a failed result with the specified error list.
    /// </summary>
    /// <param name="errors">The list of errors that occurred.</param>
    /// <returns>A failed result containing the errors.</returns>
    public static Result<T> Failure(List<Error> errors)
        => new() { IsSuccess = false, Errors = errors };
}
