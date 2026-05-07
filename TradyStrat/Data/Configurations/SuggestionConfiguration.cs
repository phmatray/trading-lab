using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Common.Domain;

namespace TradyStrat.Data.Configurations;

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
        builder.Property(s => s.PromptHash).HasMaxLength(128);
        builder.HasIndex(s => s.ForDate).IsUnique();
        builder.Ignore(s => s.OrderValueEur);
        builder.Ignore(s => s.Citations);
    }
}
