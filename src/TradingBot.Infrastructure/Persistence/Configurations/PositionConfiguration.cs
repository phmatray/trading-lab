// <copyright file="PositionConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Position.
/// </summary>
internal sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(p => p.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.Side)
            .HasColumnName("side")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(p => p.EntryPrice)
            .HasColumnName("entry_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.CurrentPrice)
            .HasColumnName("current_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.OpenedAt)
            .HasColumnName("opened_at")
            .IsRequired();

        builder.Property(p => p.StopLoss)
            .HasColumnName("stop_loss")
            .HasPrecision(18, 2);

        builder.Property(p => p.TakeProfit)
            .HasColumnName("take_profit")
            .HasPrecision(18, 2);

        builder.Property(p => p.StrategyName)
            .HasColumnName("strategy_name")
            .HasMaxLength(100)
            .IsRequired();

        // Indexes
        builder.HasIndex(p => p.Symbol).HasDatabaseName("idx_positions_symbol");
        builder.HasIndex(p => p.StrategyName).HasDatabaseName("idx_positions_strategy");
        builder.HasIndex(p => p.OpenedAt).HasDatabaseName("idx_positions_opened_at");
    }
}
