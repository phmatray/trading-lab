using TradyStrat.Domain.Shared.Money;

namespace TradyStrat.Domain;

public sealed record GrowthPoint(DateOnly Date, Money Value, Percentage ProgressPct);
