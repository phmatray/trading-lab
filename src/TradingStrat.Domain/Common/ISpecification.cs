namespace TradingStrat.Domain.Common;

/// <summary>
/// Represents a business rule specification that can be evaluated against a candidate object.
/// Implements the Specification pattern for reusable, composable business rules.
/// </summary>
/// <typeparam name="T">The type of object this specification evaluates.</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Checks whether the candidate satisfies this specification.
    /// </summary>
    /// <param name="candidate">The object to evaluate.</param>
    /// <returns>True if the candidate satisfies the specification; otherwise, false.</returns>
    bool IsSatisfiedBy(T candidate);

    /// <summary>
    /// Gets the reason why the specification was not satisfied.
    /// Empty if the specification was satisfied.
    /// </summary>
    string Reason { get; }
}
