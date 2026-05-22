using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trades");
        builder.HasKey(t => t.Id);
        // Position assigns TradeIds sequentially per-position; EF must NOT
        // generate them. See Trade.AssignId / Position.Record.
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.ExecutedOn);
        builder.Property(t => t.Side);
        builder.Property(t => t.Note).HasMaxLength(2000);
        builder.Property(t => t.CreatedAt);

        builder.OwnsOne(t => t.Quantity, q =>
        {
            q.Property(x => x.Value).HasColumnName("Quantity").HasColumnType("TEXT");
            q.Property(x => x.IsSpecified).HasColumnName("QuantityIsSpecified");
        });

        builder.OwnsOne(t => t.PricePerShare, p =>
        {
            p.OwnsOne(x => x.PerUnit, m =>
            {
                m.Property(x => x.Amount).HasColumnName("PricePerShareAmount").HasColumnType("TEXT");
                m.Property(x => x.Currency).HasConversion(c => c.Code, s => Currency.Parse(s))
                 .HasColumnName("PricePerShareCurrency").HasMaxLength(3);
                m.Property(x => x.IsEmpty).HasColumnName("PricePerShareIsEmpty");
            });
        });

        builder.OwnsOne(t => t.Fees, m =>
        {
            m.Property(x => x.Amount).HasColumnName("FeesAmount").HasColumnType("TEXT");
            m.Property(x => x.Currency).HasConversion(c => c.Code, s => Currency.Parse(s))
             .HasColumnName("FeesCurrency").HasMaxLength(3);
            m.Property(x => x.IsEmpty).HasColumnName("FeesIsEmpty");
        });

        // Denormalized for read-path back-compat during the Phase 2 cutover
        // (spec §13.1; dropped in a later cleanup migration).
        builder.Property<int>("InstrumentId");

        builder.HasIndex(t => t.ExecutedOn);
        builder.HasIndex("InstrumentId", "ExecutedOn");

        builder.Ignore(t => t.Gross);
        builder.Ignore(t => t.Net);
        builder.Ignore(t => t.IsBuy);
    }
}
