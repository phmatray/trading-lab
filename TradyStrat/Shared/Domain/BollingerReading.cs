namespace TradyStrat.Shared.Domain;

public sealed record BollingerReading(decimal Upper, decimal Middle, decimal Lower, decimal Sigma);
