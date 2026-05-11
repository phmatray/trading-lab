using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradyStrat.Common.Domain;

namespace TradyStrat.Data.Configurations;

public sealed class SettingEntryConfiguration : IEntityTypeConfiguration<SettingEntry>
{
    public void Configure(EntityTypeBuilder<SettingEntry> builder)
    {
        builder.ToTable("Settings");
        builder.HasKey(e => e.Key);
        builder.Property(e => e.Key).HasMaxLength(64);
        builder.Property(e => e.Value).IsRequired();
    }
}
