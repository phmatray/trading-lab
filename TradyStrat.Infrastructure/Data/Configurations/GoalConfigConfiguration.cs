using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class GoalConfigConfiguration : IEntityTypeConfiguration<GoalConfig>
{
    public void Configure(EntityTypeBuilder<GoalConfig> builder)
    {
        builder.ToTable("Goals");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();
        builder.Property(g => g.TargetEur).HasColumnType("TEXT");
    }
}
