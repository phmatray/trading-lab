namespace TradyStrat.Common.Domain;

public sealed record BollingerReading(decimal Upper, decimal Middle, decimal Lower, decimal Sigma);
