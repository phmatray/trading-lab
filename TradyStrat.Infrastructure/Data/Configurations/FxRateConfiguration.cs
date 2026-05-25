using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared.Money;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> builder)
    {
        builder.ToTable("FxRates");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.Date);
        builder.Property(r => r.Rate).HasColumnType("TEXT");
        builder.Property(r => r.FetchedAt);

        // CurrencyPair owned VO → existing Base/Quote string columns.
        builder.OwnsOne(r => r.Pair, p =>
        {
            p.Property(x => x.Base)
             .HasConversion(c => c.Code, s => Currency.Parse(s))
             .HasColumnName("Base").HasMaxLength(3);
            p.Property(x => x.Quote)
             .HasConversion(c => c.Code, s => Currency.Parse(s))
             .HasColumnName("Quote").HasMaxLength(3);
        });
    }
}
