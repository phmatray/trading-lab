using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TradyStrat.Domain.Goals;
using TradyStrat.Domain.Instruments;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Infrastructure.Data.Conventions;

internal static class StronglyTypedIdConventions
{
    public static void ApplyTo(ModelConfigurationBuilder builder)
    {
        builder.Properties<InstrumentId>().HaveConversion<InstrumentIdConverter>();
        builder.Properties<TradeId>     ().HaveConversion<TradeIdConverter>();
        builder.Properties<SuggestionId>().HaveConversion<SuggestionIdConverter>();
        builder.Properties<GoalId>      ().HaveConversion<GoalIdConverter>();
        builder.Properties<PositionId>  ().HaveConversion<PositionIdConverter>();
        builder.Properties<PortfolioId> ().HaveConversion<PortfolioIdConverter>();
    }

    private sealed class InstrumentIdConverter : ValueConverter<InstrumentId, int>
    {
        public InstrumentIdConverter() : base(id => id.Value, v => new InstrumentId(v)) { }
    }
    private sealed class TradeIdConverter : ValueConverter<TradeId, int>
    {
        public TradeIdConverter() : base(id => id.Value, v => new TradeId(v)) { }
    }
    private sealed class SuggestionIdConverter : ValueConverter<SuggestionId, int>
    {
        public SuggestionIdConverter() : base(id => id.Value, v => new SuggestionId(v)) { }
    }
    private sealed class GoalIdConverter : ValueConverter<GoalId, int>
    {
        public GoalIdConverter() : base(id => id.Value, v => new GoalId(v)) { }
    }
    private sealed class PositionIdConverter : ValueConverter<PositionId, int>
    {
        public PositionIdConverter() : base(id => id.Value, v => new PositionId(v)) { }
    }
    private sealed class PortfolioIdConverter : ValueConverter<PortfolioId, int>
    {
        public PortfolioIdConverter() : base(id => id.Value, v => new PortfolioId(v)) { }
    }
}
