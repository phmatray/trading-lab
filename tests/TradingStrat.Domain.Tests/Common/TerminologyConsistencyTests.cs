#pragma warning disable IDE0005 // Remove unnecessary using directives - BindingFlags is used in IsRecord method
using System.Reflection;
using Shouldly;
using TradingStrat.Domain.Entities;
using Xunit;
#pragma warning restore IDE0005

namespace TradingStrat.Domain.Tests.Common;

/// <summary>
/// Tests to enforce ubiquitous language consistency across the Domain layer.
/// These tests ensure deprecated or ambiguous terms are not introduced into the codebase.
/// See docs/GLOSSARY.md for the official terminology standards.
/// </summary>
public class TerminologyConsistencyTests
{
    private readonly Assembly _domainAssembly = typeof(Portfolio).Assembly;

    [Fact]
    public void DomainNamespace_DoesNotContainAmbiguousTerms()
    {
        // Arrange
        string[] ambiguousTerms = new[]
        {
            "Decision",      // Use "Signal" instead
            "Adjustment",    // Use "Rebalancing" instead
            "Interval",      // Use "TimeFrame" instead
            "Symbol",        // Use "Ticker" instead
            "Holding",       // Use "Position" instead
            "Account",       // Use "Portfolio" instead (unless in different context)
            "Simulation"     // Use "Backtest" instead
        };

        Type[] allTypes = _domainAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("TradingStrat.Domain") == true)
            .ToArray();

        // Act
        List<string> violations = new();

        foreach (string term in ambiguousTerms)
        {
            Type[] typesWithTerm = allTypes
                .Where(t => t.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (typesWithTerm.Any())
            {
                violations.Add($"Term '{term}' found in: {string.Join(", ", typesWithTerm.Select(t => t.Name))}");
            }
        }

        // Assert
        violations.ShouldBeEmpty($"Found ambiguous terms in Domain layer:\n{string.Join("\n", violations)}");
    }

    [Fact]
    public void DomainEntities_FollowSingularNamingConvention()
    {
        // Arrange - Entity names should be singular nouns
        Type[] entityTypes = _domainAssembly.GetTypes()
            .Where(t => t.Namespace == "TradingStrat.Domain.Entities")
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => !IsCompilerGenerated(t))
            .ToArray();

        string[] pluralExceptions = new[]
        {
            "PerformanceMetrics"  // "Metrics" is a singular collective noun (a set of measurements)
        };

        // Act
        List<string> violations = entityTypes
            .Where(t => t.Name.EndsWith("s") || t.Name.EndsWith("es"))
            .Where(t => !pluralExceptions.Contains(t.Name))
            .Select(t => t.Name)
            .ToList();

        // Assert
        violations.ShouldBeEmpty(
            $"Entity names should be singular nouns. Found plural names: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DomainEvents_FollowEventNamingConvention()
    {
        // Arrange - Events should be named [AggregateRoot][PastTenseAction]Event
        Type[] eventTypes = _domainAssembly.GetTypes()
            .Where(t => t.Namespace == "TradingStrat.Domain.Events")
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .ToArray();

        // Act
        List<string> violations = eventTypes
            .Where(t => !t.Name.EndsWith("Event"))
            .Select(t => t.Name)
            .ToList();

        // Assert
        violations.ShouldBeEmpty(
            $"Domain events must end with 'Event'. Found violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DomainExceptions_FollowExceptionNamingConvention()
    {
        // Arrange - Exceptions should end with "Exception"
        Type[] exceptionTypes = _domainAssembly.GetTypes()
            .Where(t => t.Namespace == "TradingStrat.Domain.Exceptions")
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .ToArray();

        // Act
        List<string> violations = exceptionTypes
            .Where(t => !t.Name.EndsWith("Exception"))
            .Select(t => t.Name)
            .ToList();

        // Assert
        violations.ShouldBeEmpty(
            $"Domain exceptions must end with 'Exception'. Found violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DomainSpecifications_FollowSpecificationNamingConvention()
    {
        // Arrange - Specifications should end with "Specification"
        Type[] specificationTypes = _domainAssembly.GetTypes()
            .Where(t => t.Namespace == "TradingStrat.Domain.Specifications")
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .ToArray();

        // Act
        List<string> violations = specificationTypes
            .Where(t => !t.Name.EndsWith("Specification"))
            .Select(t => t.Name)
            .ToList();

        // Assert
        violations.ShouldBeEmpty(
            $"Specifications must end with 'Specification'. Found violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DomainStrategies_FollowStrategyNamingConvention()
    {
        // Arrange - Strategies should end with "Strategy" (except interfaces and base classes)
        Type[] strategyTypes = _domainAssembly.GetTypes()
            .Where(t => t.Namespace == "TradingStrat.Domain.Strategies")
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => !IsCompilerGenerated(t))
            .Where(t => t.Name != "StrategyType" && t.Name != "StrategyDescriptor" && t.Name != "ParameterSchema")
            .ToArray();

        // Act
        List<string> violations = strategyTypes
            .Where(t => !t.Name.EndsWith("Strategy"))
            .Select(t => t.Name)
            .ToList();

        // Assert
        violations.ShouldBeEmpty(
            $"Strategy classes must end with 'Strategy'. Found violations: {string.Join(", ", violations)}");
    }

    [Fact]
    public void DomainServices_DoNotUseManagerSuffix()
    {
        // Arrange - Domain services should use descriptive suffixes (Service, Calculator, Detector)
        // "Manager" is too generic and not part of ubiquitous language
        Type[] serviceTypes = _domainAssembly.GetTypes()
            .Where(t => t.Namespace?.StartsWith("TradingStrat.Domain.Services") == true)
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .ToArray();

        // Act
        List<string> violations = serviceTypes
            .Where(t => t.Name.EndsWith("Manager", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Name)
            .ToList();

        // Assert
        violations.ShouldBeEmpty(
            $"Domain services should use specific suffixes (Service, Calculator, Detector) instead of 'Manager'. Found: {string.Join(", ", violations)}");
    }

    [Fact]
    public void ValueObjects_FollowImmutabilityConvention()
    {
        // Arrange - Value objects should be records (enforces immutability)
        Type[] valueObjectTypes = _domainAssembly.GetTypes()
            .Where(t => t.Namespace == "TradingStrat.Domain.ValueObjects")
            .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsEnum)
            .Where(t => !IsCompilerGenerated(t))
            .ToArray();

        string[] classExceptions = new[]
        {
            "MarketFeatures",        // ML.NET requires class for feature generation
            "PricePrediction",       // ML.NET requires class for predictions
            "PredictionThresholds"   // Legacy class, consider converting to record
        };

        // Act
        List<string> violations = valueObjectTypes
            .Where(t => !t.IsValueType && !IsRecord(t))
            .Where(t => !classExceptions.Contains(t.Name))
            .Select(t => t.Name)
            .ToList();

        // Assert
        violations.ShouldBeEmpty(
            $"Value objects should be records or structs for immutability. Found classes: {string.Join(", ", violations)}");
    }

    private static bool IsRecord(Type type)
    {
        // Records have a compiler-generated <Clone>$ method
        return type.GetMethod("<Clone>$", BindingFlags.Public | BindingFlags.Instance) != null;
    }

    private static bool IsCompilerGenerated(Type type)
    {
        // Compiler-generated types start with < or contain <>
        return type.Name.StartsWith("<") || type.Name.Contains("<>");
    }
}
