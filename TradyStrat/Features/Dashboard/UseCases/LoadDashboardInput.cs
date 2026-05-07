namespace TradyStrat.Features.Dashboard.UseCases;

public sealed record LoadDashboardInput(DateOnly TargetDate, bool IsHistorical);
