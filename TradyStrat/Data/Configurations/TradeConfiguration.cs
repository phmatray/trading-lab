using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trades");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();
        builder.Property(t => t.Quantity).HasColumnType("TEXT");
        builder.Property(t => t.PricePerShare).HasColumnType("TEXT");
        builder.Property(t => t.FeesEur).HasColumnType("TEXT");
        builder.Property(t => t.Note).HasMaxLength(2000);
        builder.HasOne<Instrument>()
               .WithMany()
               .HasForeignKey(t => t.InstrumentId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.ExecutedOn);
        builder.HasIndex(t => new { t.InstrumentId, t.ExecutedOn });
        builder.Ignore(t => t.GrossEur);
        builder.Ignore(t => t.NetEur);
        builder.Ignore(t => t.IsBuy);
    }
}
