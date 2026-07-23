// <copyright file="EquityPointConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Analytics;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for EquityPoint.
/// Updated to use consolidated Analytics namespace.
/// </summary>
internal sealed class EquityPointConfiguration : IEntityTypeConfiguration<EquityPoint>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<EquityPoint> builder)
    {
        builder.ToTable("equity_points");

        // Use Timestamp as primary key
        builder.HasKey(e => e.Timestamp);

        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.Property(e => e.Equity)
            .HasColumnName("equity")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(e => e.CumulativeReturn)
            .HasColumnName("cumulative_return")
            .HasPrecision(10, 4);

        builder.Property(e => e.Drawdown)
            .HasColumnName("drawdown")
            .HasPrecision(10, 4);

        builder.Property(e => e.Peak)
            .HasColumnName("peak")
            .HasPrecision(18, 2);

        builder.Property(e => e.ReturnPercent)
            .HasColumnName("return_percent")
            .HasPrecision(10, 4);

        // Index
        builder.HasIndex(e => e.Timestamp).HasDatabaseName("idx_equity_points_timestamp");
    }
}
