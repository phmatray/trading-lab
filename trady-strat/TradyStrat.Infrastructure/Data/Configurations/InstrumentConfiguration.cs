using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared.Money;
using TradyStrat.Domain.Shared.Market;

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

        builder.Property(i => i.Currency)
               .HasConversion(c => c.Code, s => Currency.Parse(s))
               .HasMaxLength(3).IsRequired();

        builder.Property(i => i.Exchange)
               .HasConversion(e => e.Code, s => Exchange.Of(s))
               .HasMaxLength(64).IsRequired();

        builder.Property(i => i.Timezone)
               .HasColumnName("TimezoneId")           // preserve existing column name
               .HasConversion(t => t.Value, s => TimezoneId.Of(s))
               .HasMaxLength(64).IsRequired();

        builder.Property(i => i.Kind).HasConversion<int>();
        builder.Property(i => i.AddedAt);
        builder.HasIndex(i => i.Ticker).IsUnique();
    }
}
