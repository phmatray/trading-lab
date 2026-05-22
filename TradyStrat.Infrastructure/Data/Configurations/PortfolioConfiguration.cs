using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortfolioAr = global::TradyStrat.Domain.Portfolio.Portfolio;
using TradyStrat.Domain.Portfolio;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class PortfolioConfiguration : IEntityTypeConfiguration<PortfolioAr>
{
    public void Configure(EntityTypeBuilder<PortfolioAr> builder)
    {
        builder.ToTable("Portfolios");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();  // singleton with explicit Id = 1

        builder.HasMany<Position>("_positions")
               .WithOne()
               .HasForeignKey("PortfolioId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_positions")
               .HasField("_positions")
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(p => p.Positions);
    }
}
