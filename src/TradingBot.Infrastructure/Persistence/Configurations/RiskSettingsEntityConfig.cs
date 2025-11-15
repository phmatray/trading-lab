// <copyright file="RiskSettingsEntityConfig.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Configuration;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for RiskSettings.
/// </summary>
public class RiskSettingsEntityConfig : IEntityTypeConfiguration<RiskSettings>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<RiskSettings> builder)
    {
        builder.ToTable("RiskSettings");

        builder.HasKey(x => x.Id);

        // Ignore domain events from SharedKernel
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.MaxPositionSizePercent)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.StopLossPercent)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.TakeProfitPercent)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.MaxOpenPositions)
            .IsRequired();

        builder.Property(x => x.MaxDailyLossPercent)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.LastModified)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Seed default row with fixed ID for singleton pattern
        builder.HasData(new RiskSettings
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            MaxPositionSizePercent = 10.0m,
            StopLossPercent = 2.0m,
            TakeProfitPercent = 5.0m,
            MaxOpenPositions = 5,
            MaxDailyLossPercent = 5.0m,
            LastModified = new DateTime(2025, 1, 14, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2025, 1, 14, 0, 0, 0, DateTimeKind.Utc),
        });
    }
}
