// <copyright file="OrderConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Trading;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Order.
/// </summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        // Ignore domain events collection (not persisted)
        builder.Ignore(o => o.DomainEvents);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(o => o.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.Side)
            .HasColumnName("side")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(o => o.LimitPrice)
            .HasColumnName("limit_price")
            .HasPrecision(18, 2);

        builder.Property(o => o.StopPrice)
            .HasColumnName("stop_price")
            .HasPrecision(18, 2);

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(o => o.SubmittedAt)
            .HasColumnName("submitted_at");

        builder.Property(o => o.FilledAt)
            .HasColumnName("filled_at");

        builder.Property(o => o.FilledQuantity)
            .HasColumnName("filled_quantity")
            .HasPrecision(18, 8)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(o => o.AverageFillPrice)
            .HasColumnName("average_fill_price")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(o => o.Commission)
            .HasColumnName("commission")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(o => o.StrategyName)
            .HasColumnName("strategy_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(o => o.SignalId)
            .HasColumnName("signal_id");

        // Indexes
        builder.HasIndex(o => o.Symbol).HasDatabaseName("idx_orders_symbol");
        builder.HasIndex(o => o.Status).HasDatabaseName("idx_orders_status");
        builder.HasIndex(o => o.StrategyName).HasDatabaseName("idx_orders_strategy");
        builder.HasIndex(o => o.CreatedAt).HasDatabaseName("idx_orders_created_at");
    }
}
