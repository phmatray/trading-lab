using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Shared.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class PriceBarConfiguration : IEntityTypeConfiguration<PriceBar>
{
    public void Configure(EntityTypeBuilder<PriceBar> builder)
    {
        builder.ToTable("PriceBars");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.Ticker).HasMaxLength(16).IsRequired();
        foreach (var col in new[] { nameof(PriceBar.Open), nameof(PriceBar.High),
                                     nameof(PriceBar.Low),  nameof(PriceBar.Close) })
            builder.Property(col).HasColumnType("TEXT");
        builder.HasIndex(p => new { p.Ticker, p.Date }).IsUnique();
        builder.Ignore(p => p.Range);
        builder.Ignore(p => p.Change);
        builder.Ignore(p => p.IsUp);
    }
}
