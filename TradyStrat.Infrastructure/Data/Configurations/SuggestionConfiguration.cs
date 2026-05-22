using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;
using TradyStrat.Domain.Suggestions;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
    public void Configure(EntityTypeBuilder<Suggestion> builder)
    {
        // Placeholder during Phase 3 rewrite — fully fleshed out in Task 11.
        // Only enough to compile against the new AR shape; the migration won't
        // apply correctly yet.
        builder.ToTable("Suggestions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.HasIndex(s => new { s.ForDate, s.InstrumentId }).IsUnique();
        builder.Ignore(s => s.Citations);
        builder.Ignore(s => s.OrderValue);
        builder.Ignore(s => s.QuantityHint);
        builder.Ignore(s => s.MaxPriceHint);
        builder.Ignore(s => s.Conviction);
        builder.Ignore(s => s.Snapshot);
        builder.Ignore(s => s.Fingerprint);
    }
}
