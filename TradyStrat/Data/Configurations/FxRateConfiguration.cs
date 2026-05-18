using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> builder)
    {
        builder.ToTable("FxRates");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.Base).HasMaxLength(3).IsRequired();
        builder.Property(r => r.Quote).HasMaxLength(3).IsRequired();
        builder.Property(r => r.Rate).HasColumnType("TEXT");
        builder.HasIndex(r => new { r.Base, r.Quote, r.Date }).IsUnique();
    }
}
