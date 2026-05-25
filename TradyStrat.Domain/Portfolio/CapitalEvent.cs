using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Domain;

/// <summary>
/// An editorial annotation on the growth chart — a trade or AI signal worth
/// remembering. Rendered as an italic Roman numeral above the chart line and
/// elaborated in the footnote rail beneath the chart.
/// </summary>
public sealed record CapitalEvent(
    DateOnly Date,
    RomanNumeralId RomanId,
    string Headline,
    string Body);
