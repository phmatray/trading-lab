// <copyright file="TradeConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Trade.
/// </summary>
internal sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("trades");

        builder.HasKey(t => t.Id);

        // Ignore domain events from SharedKernel
        builder.Ignore(t => t.DomainEvents);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(t => t.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(t => t.Side)
            .HasColumnName("side")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(t => t.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(t => t.EntryPrice)
            .HasColumnName("entry_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.ExitPrice)
            .HasColumnName("exit_price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(t => t.EntryTime)
            .HasColumnName("entry_time")
            .IsRequired();

        builder.Property(t => t.ExitTime)
            .HasColumnName("exit_time")
            .IsRequired();

        builder.Property(t => t.Commission)
            .HasColumnName("commission")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(t => t.StrategyName)
            .HasColumnName("strategy_name")
            .HasMaxLength(100)
            .IsRequired();

        // Indexes
        builder.HasIndex(t => t.Symbol).HasDatabaseName("idx_trades_symbol");
        builder.HasIndex(t => t.StrategyName).HasDatabaseName("idx_trades_strategy");
        builder.HasIndex(t => t.EntryTime).HasDatabaseName("idx_trades_entry_time");
        builder.HasIndex(t => t.ExitTime).HasDatabaseName("idx_trades_exit_time");
    }
}
