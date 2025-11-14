// <copyright file="ValidationResult.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Validators;

/// <summary>
/// Represents a validation result.
/// </summary>
public class ValidationResult
{
    private ValidationResult(bool isSuccess, IEnumerable<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors.ToList();
    }

    /// <summary>
    /// Gets a value indicating whether validation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the list of validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A successful validation result.</returns>
    public static ValidationResult Success() => new(true, []);

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    /// <returns>A failed validation result.</returns>
    public static ValidationResult Error(IEnumerable<string> errors) => new(false, errors);
}
