namespace TradingStrat.Domain.Common;

/// <summary>
/// Represents a void type, since void cannot be used as a generic type parameter.
/// Used for use cases that don't require a command parameter.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    /// Gets the single value of the Unit type.
    /// </summary>
    public static Unit Value => default;

    public bool Equals(Unit other) => true;

    public override bool Equals(object? obj) => obj is Unit;

    public override int GetHashCode() => 0;

    public static bool operator ==(Unit left, Unit right) => true;

    public static bool operator !=(Unit left, Unit right) => false;

    public override string ToString() => "()";
}
