namespace TradyStrat.Domain;

/// <summary>One row of the key/value Settings table. Key is the EF primary key.</summary>
public sealed record SettingEntry
{
    public required string Key { get; init; }
    public required string Value { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
