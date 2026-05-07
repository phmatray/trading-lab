using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Common.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> builder)
    {
        builder.ToTable("FxRates");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.Pair).HasMaxLength(8).IsRequired();
        builder.Property(r => r.UsdPerEur).HasColumnType("TEXT");
        builder.HasIndex(r => new { r.Pair, r.Date }).IsUnique();
        builder.Ignore(r => r.EurPerUsd);
    }
}
