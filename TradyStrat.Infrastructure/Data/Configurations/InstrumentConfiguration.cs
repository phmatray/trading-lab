using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("Instruments");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();
        builder.Property(i => i.Ticker).HasMaxLength(16).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Currency).HasMaxLength(3).IsRequired();
        builder.Property(i => i.Exchange).HasMaxLength(64).IsRequired();
        builder.Property(i => i.TimezoneId).HasMaxLength(64).IsRequired();
        builder.Property(i => i.Kind).HasConversion<int>();
        builder.HasIndex(i => i.Ticker).IsUnique();
    }
}
