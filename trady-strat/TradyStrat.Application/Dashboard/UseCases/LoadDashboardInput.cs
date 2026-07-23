namespace TradyStrat.Application.Dashboard.UseCases;

public sealed record LoadDashboardInput(DateOnly TargetDate, bool IsHistorical);
