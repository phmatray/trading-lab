using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

public sealed record GrowthPoint(DateOnly Date, Money Value, Percentage ProgressPct);
