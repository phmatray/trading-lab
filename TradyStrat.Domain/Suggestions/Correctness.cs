using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain.Suggestions;

public sealed record Correctness(bool IsCorrect, Money ForwardReturn);
