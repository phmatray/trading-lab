// <copyright file="WeeklyCashManagedStrategyConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Strategy;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for WeeklyCashManagedStrategy.
/// </summary>
internal sealed class WeeklyCashManagedStrategyConfiguration : IEntityTypeConfiguration<WeeklyCashManagedStrategy>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<WeeklyCashManagedStrategy> builder)
    {
        builder.ToTable("weekly_cash_managed_strategies");

        builder.HasKey(s => s.Id);

        // Ignore domain events collection (not persisted)
        builder.Ignore(s => s.DomainEvents);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .IsRequired();

        builder.Property(s => s.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.EtpSymbol)
            .HasColumnName("etp_symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.UnderlyingSymbol)
            .HasColumnName("underlying_symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.Property(s => s.MinCashRatio)
            .HasColumnName("min_cash_ratio")
            .HasPrecision(5, 4) // e.g., 0.1500 (15%)
            .IsRequired();

        builder.Property(s => s.MaxCashRatio)
            .HasColumnName("max_cash_ratio")
            .HasPrecision(5, 4) // e.g., 0.2500 (25%)
            .IsRequired();

        builder.Property(s => s.WeeklyBuyRatio)
            .HasColumnName("weekly_buy_ratio")
            .HasPrecision(5, 4) // e.g., 0.0500 (5%)
            .IsRequired();

        builder.Property(s => s.WeeklySellRatio)
            .HasColumnName("weekly_sell_ratio")
            .HasPrecision(5, 4) // e.g., 0.1000 (10%)
            .IsRequired();

        builder.Property(s => s.ExecutionDayOfWeek)
            .HasColumnName("execution_day_of_week")
            .IsRequired();

        builder.Property(s => s.DaysBelowMA20)
            .HasColumnName("days_below_ma20")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.LastExecutionTimestamp)
            .HasColumnName("last_execution_timestamp");

        builder.Property(s => s.LastDailyUpdateTimestamp)
            .HasColumnName("last_daily_update_timestamp");

        builder.Property(s => s.CurrentMA20)
            .HasColumnName("current_ma20")
            .HasPrecision(18, 2);

        builder.Property(s => s.CurrentUnderlyingPrice)
            .HasColumnName("current_underlying_price")
            .HasPrecision(18, 2);

        builder.Property(s => s.CurrentEtpPrice)
            .HasColumnName("current_etp_price")
            .HasPrecision(18, 2);

        builder.Property(s => s.BreakoutRuleConfigJson)
            .HasColumnName("breakout_rule_config_json")
            .HasColumnType("TEXT"); // SQLite JSON storage

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(s => s.LastModified)
            .HasColumnName("last_modified");

        // Indexes
        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("idx_wcm_strategy_name");

        builder.HasIndex(s => s.EtpSymbol)
            .HasDatabaseName("idx_wcm_strategy_etp_symbol");

        builder.HasIndex(s => s.UnderlyingSymbol)
            .HasDatabaseName("idx_wcm_strategy_underlying_symbol");

        builder.HasIndex(s => s.IsEnabled)
            .HasDatabaseName("idx_wcm_strategy_is_enabled");

        builder.HasIndex(s => s.LastExecutionTimestamp)
            .HasDatabaseName("idx_wcm_strategy_last_execution");
    }
}
