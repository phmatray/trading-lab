using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Domain;
using TradyStrat.Domain.Shared;

namespace TradyStrat.Infrastructure.Data.Configurations;

public sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    public void Configure(EntityTypeBuilder<Goal> builder)
    {
        builder.ToTable("Goals");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        // Money owned VO → existing TargetEur column for Amount; new shadow columns
        // for Currency + IsEmpty. If the live DB lacks them, EF will throw at first
        // read — generate a one-shot AddGoalMoneyColumns migration in that case
        // (Task 17.4).
        builder.OwnsOne(g => g.Target, m =>
        {
            m.Property(x => x.Amount)
             .HasColumnName("TargetEur")
             .HasColumnType("TEXT");
            m.Property(x => x.Currency)
             .HasConversion(c => c.Code, s => Currency.Parse(s))
             .HasColumnName("TargetCurrency")
             .HasMaxLength(3);
            m.Property(x => x.IsEmpty).HasColumnName("TargetIsEmpty");
        });

        // TargetDate: was nullable DateOnly?, now non-nullable with DateOnly.MinValue
        // as sentinel. Legacy NULL rows rehydrate as MinValue via the converter.
        builder.Property(g => g.TargetDate)
               .HasConversion(
                   d => d == DateOnly.MinValue ? (DateOnly?)null : d,
                   d => d ?? DateOnly.MinValue)
               .HasColumnName("TargetDate");

        builder.Property(g => g.UpdatedAt);
    }
}
