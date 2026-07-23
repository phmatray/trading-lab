namespace TradingStrat.Application.Services;

/// <summary>
/// Metadata for a strategy parameter including default, min, max, and display information.
/// </summary>
public sealed record ParameterMetadata
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public required object DefaultValue { get; init; }
    public required object? MinValue { get; init; }
    public required object? MaxValue { get; init; }
    public required string Type { get; init; } // "int", "decimal", "enum"

    public T GetDefault<T>() => (T)DefaultValue;
    public T? GetMin<T>() where T : struct => MinValue is not null ? (T)MinValue : null;
    public T? GetMax<T>() where T : struct => MaxValue is not null ? (T)MaxValue : null;

    public (T Min, T Max) GetRange<T>() where T : struct
    {
        if (MinValue is null || MaxValue is null)
        {
            throw new InvalidOperationException($"Parameter {Name} does not have min/max range defined");
        }

        return ((T)MinValue, (T)MaxValue);
    }
}

/// <summary>
/// Collection of parameter metadata for a strategy.
/// </summary>
public sealed class StrategyParameters
{
    private readonly Dictionary<string, ParameterMetadata> _parameters = new();

    public void Add(ParameterMetadata parameter)
    {
        _parameters[parameter.Name] = parameter;
    }

    public ParameterMetadata Get(string name)
    {
        if (!_parameters.TryGetValue(name, out ParameterMetadata? parameter))
        {
            throw new KeyNotFoundException($"Parameter '{name}' not found");
        }

        return parameter;
    }

    public bool TryGet(string name, out ParameterMetadata? parameter)
    {
        return _parameters.TryGetValue(name, out parameter);
    }

    public IReadOnlyDictionary<string, ParameterMetadata> GetAll() => _parameters;

    public Dictionary<string, object> GetDefaults()
    {
        return _parameters.ToDictionary(
            p => p.Key,
            p => p.Value.DefaultValue
        );
    }
}
