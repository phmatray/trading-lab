namespace TradyStrat.Application.PriceFeed.UseCases;

public sealed record GetPriceSeriesInput(string Ticker, DateOnly From, DateOnly To, bool WithIndicators);
