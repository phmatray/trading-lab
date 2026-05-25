using TradyStrat.Domain.Shared.Money;

namespace TradyStrat.Domain.Suggestions;

public sealed record Correctness(bool IsCorrect, Money ForwardReturn);
