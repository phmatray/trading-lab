using System.Runtime.CompilerServices;

namespace TradingStrat.Domain.Common;

/// <summary>
/// Fluent validation guard for domain entity and value object construction.
/// Provides expressive, chainable validation with clear error messages.
/// </summary>
public static class ValidationGuard
{
    /// <summary>
    /// Starts a validation chain for a parameter.
    /// Automatically captures the parameter name using CallerArgumentExpression.
    /// </summary>
    /// <typeparam name="T">The type of the parameter being validated.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="parameterName">The name of the parameter (automatically captured).</param>
    /// <returns>A ValidationContext for chaining validations.</returns>
    public static ValidationContext<T> Require<T>(
        T value,
        [CallerArgumentExpression(nameof(value))] string? parameterName = null)
    {
        return new ValidationContext<T>(value, parameterName ?? "value");
    }

    /// <summary>
    /// Ensures a condition is true, throwing ArgumentException if false.
    /// Use for simple one-off validations without chaining.
    /// </summary>
    /// <param name="condition">The condition that must be true.</param>
    /// <param name="message">The error message if the condition is false.</param>
    /// <param name="parameterName">Optional parameter name for the exception.</param>
    /// <exception cref="ArgumentException">Thrown when condition is false.</exception>
    public static void Require(bool condition, string message, string? parameterName = null)
    {
        if (!condition)
        {
            throw new ArgumentException(message, parameterName);
        }
    }
}

/// <summary>
/// Validation context for fluent chaining of validation rules.
/// </summary>
/// <typeparam name="T">The type of the value being validated.</typeparam>
public class ValidationContext<T>
{
    private readonly T _value;
    private readonly string _parameterName;

    internal ValidationContext(T value, string parameterName)
    {
        _value = value;
        _parameterName = parameterName;
    }

    /// <summary>
    /// Validates that the value is not null.
    /// </summary>
    /// <returns>This context for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public ValidationContext<T> NotNull()
    {
        if (_value == null)
        {
            throw new ArgumentNullException(_parameterName);
        }
        return this;
    }

    /// <summary>
    /// Validates that a numeric value is greater than a minimum.
    /// </summary>
    /// <param name="minimum">The minimum allowed value (exclusive).</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>This context for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when value is not greater than minimum.</exception>
    public ValidationContext<T> GreaterThan<TValue>(TValue minimum, string? message = null)
        where TValue : IComparable<TValue>
    {
        if (_value is IComparable<TValue> comparable && comparable.CompareTo(minimum) <= 0)
        {
            string errorMessage = message ?? $"{_parameterName} must be greater than {minimum}";
            throw new ArgumentException(errorMessage, _parameterName);
        }
        return this;
    }

    /// <summary>
    /// Validates that a numeric value is greater than or equal to a minimum.
    /// </summary>
    /// <param name="minimum">The minimum allowed value (inclusive).</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>This context for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when value is less than minimum.</exception>
    public ValidationContext<T> GreaterThanOrEqual<TValue>(TValue minimum, string? message = null)
        where TValue : IComparable<TValue>
    {
        if (_value is IComparable<TValue> comparable && comparable.CompareTo(minimum) < 0)
        {
            string errorMessage = message ?? $"{_parameterName} must be greater than or equal to {minimum}";
            throw new ArgumentException(errorMessage, _parameterName);
        }
        return this;
    }

    /// <summary>
    /// Validates that a numeric value is less than a maximum.
    /// </summary>
    /// <param name="maximum">The maximum allowed value (exclusive).</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>This context for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when value is not less than maximum.</exception>
    public ValidationContext<T> LessThan<TValue>(TValue maximum, string? message = null)
        where TValue : IComparable<TValue>
    {
        if (_value is IComparable<TValue> comparable && comparable.CompareTo(maximum) >= 0)
        {
            string errorMessage = message ?? $"{_parameterName} must be less than {maximum}";
            throw new ArgumentException(errorMessage, _parameterName);
        }
        return this;
    }

    /// <summary>
    /// Validates that a numeric value is less than or equal to a maximum.
    /// </summary>
    /// <param name="maximum">The maximum allowed value (inclusive).</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>This context for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when value is greater than maximum.</exception>
    public ValidationContext<T> LessThanOrEqual<TValue>(TValue maximum, string? message = null)
        where TValue : IComparable<TValue>
    {
        if (_value is IComparable<TValue> comparable && comparable.CompareTo(maximum) > 0)
        {
            string errorMessage = message ?? $"{_parameterName} must be less than or equal to {maximum}";
            throw new ArgumentException(errorMessage, _parameterName);
        }
        return this;
    }

    /// <summary>
    /// Validates that a numeric value is within an inclusive range.
    /// </summary>
    /// <param name="minimum">The minimum allowed value (inclusive).</param>
    /// <param name="maximum">The maximum allowed value (inclusive).</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>This context for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when value is outside the range.</exception>
    public ValidationContext<T> InRange<TValue>(TValue minimum, TValue maximum, string? message = null)
        where TValue : IComparable<TValue>
    {
        if (_value is IComparable<TValue> comparable)
        {
            if (comparable.CompareTo(minimum) < 0 || comparable.CompareTo(maximum) > 0)
            {
                string errorMessage = message ?? $"{_parameterName} must be between {minimum} and {maximum}";
                throw new ArgumentException(errorMessage, _parameterName);
            }
        }
        return this;
    }

    /// <summary>
    /// Validates a custom condition.
    /// </summary>
    /// <param name="condition">The condition that must be true.</param>
    /// <param name="message">The error message if the condition is false.</param>
    /// <returns>This context for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when condition is false.</exception>
    public ValidationContext<T> Satisfies(bool condition, string message)
    {
        if (!condition)
        {
            throw new ArgumentException(message, _parameterName);
        }
        return this;
    }
}
