namespace TradingStrat.Domain.Common;

/// <summary>
/// Base class for value objects that provides value-based equality semantics.
/// Value objects are immutable objects whose equality is based on their property values
/// rather than object identity.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the components that are used for equality comparison.
    /// Derived classes must yield all properties that should be included in equality checks.
    /// </summary>
    /// <returns>An enumerable of objects representing the equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Determines whether the specified object is equal to the current object
    /// by comparing all equality components.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Serves as the default hash function, computed from all equality components.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }

    /// <summary>
    /// Determines whether the specified ValueObject is equal to the current ValueObject.
    /// </summary>
    /// <param name="other">The ValueObject to compare with the current object.</param>
    /// <returns>true if the specified ValueObject is equal to the current object; otherwise, false.</returns>
    public bool Equals(ValueObject? other)
    {
        return Equals((object?)other);
    }

    /// <summary>
    /// Determines whether two ValueObject instances are equal.
    /// </summary>
    /// <param name="left">The first ValueObject to compare.</param>
    /// <param name="right">The second ValueObject to compare.</param>
    /// <returns>true if the two instances are equal; otherwise, false.</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two ValueObject instances are not equal.
    /// </summary>
    /// <param name="left">The first ValueObject to compare.</param>
    /// <param name="right">The second ValueObject to compare.</param>
    /// <returns>true if the two instances are not equal; otherwise, false.</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}
