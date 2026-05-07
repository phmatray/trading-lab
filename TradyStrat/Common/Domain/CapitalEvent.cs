namespace TradyStrat.Common.Domain;

/// <summary>
/// An editorial annotation on the growth chart — a trade or AI signal worth
/// remembering. Rendered as an italic Roman numeral above the chart line and
/// elaborated in the footnote rail beneath the chart.
/// </summary>
/// <param name="Date">When the event happened.</param>
/// <param name="RomanId">Lowercase Roman numeral identifier ("i", "ii", "iii", "iv").</param>
/// <param name="Headline">Short italic lede (e.g. "Initial position.")</param>
/// <param name="Body">Reasoning / context sentence.</param>
public sealed record CapitalEvent(
    DateOnly Date,
    string RomanId,
    string Headline,
    string Body);
