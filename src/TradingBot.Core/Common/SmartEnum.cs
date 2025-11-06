// <copyright file="SmartEnum.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Core.Common;

/// <summary>
/// Base class for creating type-safe enumeration classes with value and name properties.
/// Provides a strongly-typed alternative to traditional enums with additional functionality.
/// </summary>
/// <typeparam name="TEnum">The derived enumeration type.</typeparam>
/// <typeparam name="TValue">The type of the underlying value (typically int or string).</typeparam>
public abstract class SmartEnum<TEnum, TValue> : IEquatable<SmartEnum<TEnum, TValue>>, IComparable<SmartEnum<TEnum, TValue>>
    where TEnum : SmartEnum<TEnum, TValue>
    where TValue : IEquatable<TValue>, IComparable<TValue>
{
    private static readonly Lazy<Dictionary<TValue, TEnum>> ValueDictionary = new(() =>
    {
        var enumType = typeof(TEnum);
        var fields = enumType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        return fields
            .Where(f => f.FieldType == enumType)
            .Select(f => (TEnum)f.GetValue(null)!)
            .ToDictionary(e => e.Value);
    });

    private static readonly Lazy<Dictionary<string, TEnum>> NameDictionary = new(() =>
    {
        return ValueDictionary.Value.Values.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase);
    });

    /// <summary>
    /// Initializes a new instance of the <see cref="SmartEnum{TEnum, TValue}"/> class.
    /// </summary>
    /// <param name="name">The name of the enumeration value.</param>
    /// <param name="value">The underlying value.</param>
    protected SmartEnum(string name, TValue value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets the name of the enumeration value.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the underlying value.
    /// </summary>
    public TValue Value { get; }

    /// <summary>
    /// Implicitly converts a SmartEnum to its underlying value.
    /// </summary>
    /// <param name="smartEnum">The SmartEnum to convert.</param>
    public static implicit operator TValue(SmartEnum<TEnum, TValue> smartEnum)
    {
        return smartEnum.Value;
    }

    /// <summary>
    /// Determines whether two SmartEnum values are equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True if the values are equal; otherwise, false.</returns>
    public static bool operator ==(SmartEnum<TEnum, TValue>? left, SmartEnum<TEnum, TValue>? right)
    {
        if (left is null)
        {
            return right is null;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two SmartEnum values are not equal.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True if the values are not equal; otherwise, false.</returns>
    public static bool operator !=(SmartEnum<TEnum, TValue>? left, SmartEnum<TEnum, TValue>? right)
    {
        return !(left == right);
    }

    /// <summary>
    /// Determines whether one SmartEnum value is less than another.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True if the left value is less than the right value; otherwise, false.</returns>
    public static bool operator <(SmartEnum<TEnum, TValue>? left, SmartEnum<TEnum, TValue>? right)
    {
        return left is not null && left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Determines whether one SmartEnum value is less than or equal to another.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True if the left value is less than or equal to the right value; otherwise, false.</returns>
    public static bool operator <=(SmartEnum<TEnum, TValue>? left, SmartEnum<TEnum, TValue>? right)
    {
        return left is null || left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Determines whether one SmartEnum value is greater than another.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True if the left value is greater than the right value; otherwise, false.</returns>
    public static bool operator >(SmartEnum<TEnum, TValue>? left, SmartEnum<TEnum, TValue>? right)
    {
        return left is not null && left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Determines whether one SmartEnum value is greater than or equal to another.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>True if the left value is greater than or equal to the right value; otherwise, false.</returns>
    public static bool operator >=(SmartEnum<TEnum, TValue>? left, SmartEnum<TEnum, TValue>? right)
    {
        return left is null ? right is null : left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Gets all defined enumeration values.
    /// </summary>
    /// <returns>A collection of all enumeration values.</returns>
    public static IEnumerable<TEnum> GetAll()
    {
        return ValueDictionary.Value.Values;
    }

    /// <summary>
    /// Gets an enumeration value by its underlying value.
    /// </summary>
    /// <param name="value">The underlying value to search for.</param>
    /// <returns>The enumeration value if found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is not found.</exception>
    public static TEnum FromValue(TValue value)
    {
        if (ValueDictionary.Value.TryGetValue(value, out var enumValue))
        {
            return enumValue;
        }

        throw new InvalidOperationException($"No {typeof(TEnum).Name} with value {value} found.");
    }

    /// <summary>
    /// Tries to get an enumeration value by its underlying value.
    /// </summary>
    /// <param name="value">The underlying value to search for.</param>
    /// <param name="result">The enumeration value if found.</param>
    /// <returns>True if the value was found; otherwise, false.</returns>
    public static bool TryFromValue(TValue value, out TEnum? result)
    {
        return ValueDictionary.Value.TryGetValue(value, out result);
    }

    /// <summary>
    /// Gets an enumeration value by its name.
    /// </summary>
    /// <param name="name">The name to search for (case-insensitive).</param>
    /// <returns>The enumeration value if found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the name is not found.</exception>
    public static TEnum FromName(string name)
    {
        if (NameDictionary.Value.TryGetValue(name, out var enumValue))
        {
            return enumValue;
        }

        throw new InvalidOperationException($"No {typeof(TEnum).Name} with name '{name}' found.");
    }

    /// <summary>
    /// Tries to get an enumeration value by its name.
    /// </summary>
    /// <param name="name">The name to search for (case-insensitive).</param>
    /// <param name="result">The enumeration value if found.</param>
    /// <returns>True if the name was found; otherwise, false.</returns>
    public static bool TryFromName(string name, out TEnum? result)
    {
        return NameDictionary.Value.TryGetValue(name, out result);
    }

    /// <summary>
    /// Returns a string representation of the enumeration value.
    /// </summary>
    /// <returns>The name of the enumeration value.</returns>
    public override string ToString() => Name;

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is SmartEnum<TEnum, TValue> other && Equals(other);
    }

    /// <inheritdoc/>
    public bool Equals(SmartEnum<TEnum, TValue>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return GetType() == other.GetType() && Value.Equals(other.Value);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Value);
    }

    /// <inheritdoc/>
    public int CompareTo(SmartEnum<TEnum, TValue>? other)
    {
        if (other is null)
        {
            return 1;
        }

        return Value.CompareTo(other.Value);
    }
}
