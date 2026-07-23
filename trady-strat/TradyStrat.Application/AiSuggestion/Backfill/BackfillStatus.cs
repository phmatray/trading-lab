namespace TradyStrat.Application.AiSuggestion.Backfill;

public abstract record BackfillStatus
{
    public sealed record Idle : BackfillStatus
    {
        public static readonly Idle Instance = new();
        private Idle() { }
    }

    public sealed record Running(int Remaining, int Total, DateOnly CurrentDate) : BackfillStatus;

    public sealed record Failed(DateOnly LastSuccessful, DateOnly FailedAt, string Reason)
        : BackfillStatus;
}
