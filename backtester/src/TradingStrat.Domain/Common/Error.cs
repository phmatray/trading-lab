namespace TradingStrat.Domain.Common;

/// <summary>
/// Represents an error that occurred during domain operation.
/// Immutable record for type-safe error handling with Result pattern.
/// </summary>
public sealed record Error
{
    /// <summary>
    /// Gets the type of error that occurred.
    /// </summary>
    public ErrorType Type { get; init; }

    /// <summary>
    /// Gets the error code for programmatic handling.
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// Gets the human-readable error message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Creates a validation error.
    /// </summary>
    public static Error Validation(string message, string code = "VALIDATION_ERROR")
        => new() { Type = ErrorType.Validation, Code = code, Message = message };

    /// <summary>
    /// Creates a not found error.
    /// </summary>
    public static Error NotFound(string message, string code = "NOT_FOUND")
        => new() { Type = ErrorType.NotFound, Code = code, Message = message };

    /// <summary>
    /// Creates a business rule violation error.
    /// </summary>
    public static Error BusinessRule(string message, string code = "BUSINESS_RULE_VIOLATION")
        => new() { Type = ErrorType.BusinessRule, Code = code, Message = message };

    /// <summary>
    /// Creates a conflict error (e.g., duplicate resource).
    /// </summary>
    public static Error Conflict(string message, string code = "CONFLICT")
        => new() { Type = ErrorType.Conflict, Code = code, Message = message };

    /// <summary>
    /// Creates an insufficient data error.
    /// </summary>
    public static Error InsufficientData(string message, string code = "INSUFFICIENT_DATA")
        => new() { Type = ErrorType.InsufficientData, Code = code, Message = message };
}

/// <summary>
/// Defines the types of errors that can occur in the domain.
/// </summary>
public enum ErrorType
{
    /// <summary>
    /// Input validation failed.
    /// </summary>
    Validation,

    /// <summary>
    /// Requested resource not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// Business rule was violated.
    /// </summary>
    BusinessRule,

    /// <summary>
    /// Resource conflict (e.g., duplicate).
    /// </summary>
    Conflict,

    /// <summary>
    /// Insufficient data to perform operation.
    /// </summary>
    InsufficientData
}
