using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain.Portfolio;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.InstrumentId);

        // RealizedPnL as owned Money (three columns: Amount, Currency, IsEmpty).
        builder.OwnsOne<Money>("_realizedPnL", m =>
        {
            m.Property(x => x.Amount).HasColumnName("RealizedPnLAmount").HasColumnType("TEXT");
            m.Property(x => x.Currency).HasConversion(c => c.Code, s => Currency.Parse(s))
             .HasColumnName("RealizedPnLCurrency").HasMaxLength(3);
            m.Property(x => x.IsEmpty).HasColumnName("RealizedPnLIsEmpty");
        });

        // Open lots as owned-many.
        builder.OwnsMany<Lot>("_openLots", lots =>
        {
            lots.ToTable("PositionLots");
            lots.WithOwner().HasForeignKey("PositionId");
            lots.Property<int>("Id").ValueGeneratedOnAdd();
            lots.HasKey("Id");

            lots.Property(l => l.OpenedOn);
            lots.OwnsOne(l => l.Quantity, q =>
            {
                q.Property(x => x.Value).HasColumnName("Quantity").HasColumnType("TEXT");
                q.Property(x => x.IsSpecified).HasColumnName("QuantityIsSpecified");
            });
            lots.OwnsOne(l => l.UnitCost, m =>
            {
                m.Property(x => x.Amount).HasColumnName("UnitCostAmount").HasColumnType("TEXT");
                m.Property(x => x.Currency).HasConversion(c => c.Code, s => Currency.Parse(s))
                 .HasColumnName("UnitCostCurrency").HasMaxLength(3);
                m.Property(x => x.IsEmpty).HasColumnName("UnitCostIsEmpty");
            });
        });

        // Trades as a relationship (separate Trades table, FK PositionId).
        builder.HasMany<Trade>("_trades")
               .WithOne()
               .HasForeignKey("PositionId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_openLots")
               .HasField("_openLots")
               .UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation("_trades")
               .HasField("_trades")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.OpenLots);
        builder.Ignore(p => p.Trades);
        builder.Ignore(p => p.RealizedPnL);
        builder.Ignore(p => p.TotalQuantity);
        builder.Ignore(p => p.CostBasis);
    }
}
