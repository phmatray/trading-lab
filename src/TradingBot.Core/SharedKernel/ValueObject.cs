// <copyright file="ValueObject.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

#pragma warning disable S2328 // GetHashCode may reference mutable fields for caching purposes

namespace TradingBot.Core.SharedKernel;

/// <summary>
/// Base class for value objects. Implements equality and comparison based on component values.
/// Note: Consider using readonly record struct for C# 10+ or Vogen for strongly-typed value objects.
/// Reference: https://enterprisecraftsmanship.com/posts/value-object-better-implementation/.
/// </summary>
public abstract class ValueObject : IComparable, IComparable<ValueObject>
{
    private int? _cachedHashCode;

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="a">First value object.</param>
    /// <param name="b">Second value object.</param>
    /// <returns>True if equal, false otherwise.</returns>
    public static bool operator ==(ValueObject? a, ValueObject? b)
    {
        if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
        {
            return true;
        }

        if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
        {
            return false;
        }

        return a.Equals(b);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="a">First value object.</param>
    /// <param name="b">Second value object.</param>
    /// <returns>True if not equal, false otherwise.</returns>
    public static bool operator !=(ValueObject? a, ValueObject? b)
    {
        return !(a == b);
    }

    /// <summary>
    /// Less than operator.
    /// </summary>
    /// <param name="a">First value object.</param>
    /// <param name="b">Second value object.</param>
    /// <returns>True if a is less than b, false otherwise.</returns>
    public static bool operator <(ValueObject? a, ValueObject? b)
    {
        if (a is null)
        {
            return b is not null;
        }

        return a.CompareTo(b) < 0;
    }

    /// <summary>
    /// Less than or equal operator.
    /// </summary>
    /// <param name="a">First value object.</param>
    /// <param name="b">Second value object.</param>
    /// <returns>True if a is less than or equal to b, false otherwise.</returns>
    public static bool operator <=(ValueObject? a, ValueObject? b)
    {
        return a is null || a.CompareTo(b) <= 0;
    }

    /// <summary>
    /// Greater than operator.
    /// </summary>
    /// <param name="a">First value object.</param>
    /// <param name="b">Second value object.</param>
    /// <returns>True if a is greater than b, false otherwise.</returns>
    public static bool operator >(ValueObject? a, ValueObject? b)
    {
        if (a is null)
        {
            return false;
        }

        return a.CompareTo(b) > 0;
    }

    /// <summary>
    /// Greater than or equal operator.
    /// </summary>
    /// <param name="a">First value object.</param>
    /// <param name="b">Second value object.</param>
    /// <returns>True if a is greater than or equal to b, false otherwise.</returns>
    public static bool operator >=(ValueObject? a, ValueObject? b)
    {
        return a is null ? b is null : a.CompareTo(b) >= 0;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (GetUnproxiedType(this) != GetUnproxiedType(obj))
        {
            return false;
        }

        var valueObject = (ValueObject)obj;

        return GetEqualityComponents().SequenceEqual(valueObject.GetEqualityComponents());
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        if (!_cachedHashCode.HasValue)
        {
            _cachedHashCode = GetEqualityComponents()
              .Aggregate(1, (current, obj) =>
              {
                  unchecked
                  {
                      return (current * 23) + (obj?.GetHashCode() ?? 0);
                  }
              });
        }

        return _cachedHashCode.Value;
    }

    /// <inheritdoc/>
    public virtual int CompareTo(object? obj)
    {
        if (obj == null)
        {
            return 1;
        }

        var thisType = GetUnproxiedType(this);
        var otherType = GetUnproxiedType(obj);

        if (thisType != otherType)
        {
            return string.Compare(thisType.ToString(), otherType.ToString(), StringComparison.Ordinal);
        }

        var other = (ValueObject)obj;

        object?[] components = GetEqualityComponents().ToArray();
        object?[] otherComponents = other.GetEqualityComponents().ToArray();

        for (int i = 0; i < components.Length; i++)
        {
            int comparison = CompareComponents(components[i], otherComponents[i]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    /// <inheritdoc/>
    public virtual int CompareTo(ValueObject? other)
    {
        return CompareTo(other as object);
    }

    /// <summary>
    /// Gets the unproxied type for Entity Framework or NHibernate proxies.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>The actual type without proxy wrapper.</returns>
    internal static Type GetUnproxiedType(object obj)
    {
        const string EFCoreProxyPrefix = "Castle.Proxies.";
        const string NHibernateProxyPostfix = "Proxy";

        Type type = obj.GetType();
        string typeString = type.ToString();

        if (typeString.Contains(EFCoreProxyPrefix) || typeString.EndsWith(NHibernateProxyPostfix))
        {
            return type.BaseType!;
        }

        return type;
    }

    /// <summary>
    /// Gets the components used for equality comparison.
    /// </summary>
    /// <returns>An enumerable of components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    private static int CompareComponents(object? object1, object? object2)
    {
        if (object1 is null && object2 is null)
        {
            return 0;
        }

        if (object1 is null)
        {
            return -1;
        }

        if (object2 is null)
        {
            return 1;
        }

        if (object1 is IComparable comparable1 && object2 is IComparable comparable2)
        {
            return comparable1.CompareTo(comparable2);
        }

        return object1.Equals(object2) ? 0 : -1;
    }
}
