// <copyright file="CandleConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.MarketData;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Candle.
/// </summary>
internal sealed class CandleConfiguration : IEntityTypeConfiguration<Candle>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Candle> builder)
    {
        builder.ToTable("candles");

        // Composite key: Symbol + Timestamp + Timeframe
        builder.HasKey(c => new { c.Symbol, c.Timestamp, c.Timeframe });

        builder.Property(c => c.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.Property(c => c.Open)
            .HasColumnName("open")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.High)
            .HasColumnName("high")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.Low)
            .HasColumnName("low")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.Close)
            .HasColumnName("close")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(c => c.Volume)
            .HasColumnName("volume")
            .IsRequired();

        builder.Property(c => c.Timeframe)
            .HasColumnName("timeframe")
            .HasMaxLength(10)
            .IsRequired();

        // Indexes
        builder.HasIndex(c => c.Symbol).HasDatabaseName("idx_candles_symbol");
        builder.HasIndex(c => c.Timestamp).HasDatabaseName("idx_candles_timestamp");
        builder.HasIndex(c => new { c.Symbol, c.Timeframe }).HasDatabaseName("idx_candles_symbol_timeframe");
    }
}
