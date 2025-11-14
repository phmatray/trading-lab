// <copyright file="RiskSettingsEntityConfig.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Configuration;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for RiskSettings entity.
/// </summary>
public class RiskSettingsEntityConfig : IEntityTypeConfiguration<RiskSettings>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RiskSettings> builder)
    {
        builder.ToTable("RiskSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.MaxPositionSizePercent)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(10m);

        builder.Property(x => x.StopLossPercent)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(2m);

        builder.Property(x => x.TakeProfitPercent)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(5m);

        builder.Property(x => x.MaxOpenPositions)
            .IsRequired()
            .HasDefaultValue(5);

        builder.Property(x => x.MaxDailyLossPercent)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(5m);

        builder.Property(x => x.LastModified)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Seed default row (use static dates for deterministic migrations)
        builder.HasData(new RiskSettings
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            MaxPositionSizePercent = 10m,
            StopLossPercent = 2m,
            TakeProfitPercent = 5m,
            MaxOpenPositions = 5,
            MaxDailyLossPercent = 5m,
            LastModified = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}
