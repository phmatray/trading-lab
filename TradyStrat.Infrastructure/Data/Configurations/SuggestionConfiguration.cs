using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class SuggestionConfiguration : IEntityTypeConfiguration<Suggestion>
{
    public void Configure(EntityTypeBuilder<Suggestion> builder)
    {
        builder.ToTable("Suggestions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.QuantityHint).HasColumnType("TEXT");
        builder.Property(s => s.MaxPriceHint).HasColumnType("TEXT");
        builder.Property(s => s.Rationale).HasMaxLength(4000);
        builder.Property(s => s.CitationsJson).HasMaxLength(8000);
        builder.Property(s => s.MarketSnapshotJson).HasMaxLength(8000);
        builder.Property(s => s.PromptHash).HasMaxLength(128);
        builder.HasOne<Instrument>()
               .WithMany()
               .HasForeignKey(s => s.InstrumentId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.ForDate, s.InstrumentId }).IsUnique();
        builder.Ignore(s => s.OrderValueEur);
        builder.Ignore(s => s.Citations);
    }
}
