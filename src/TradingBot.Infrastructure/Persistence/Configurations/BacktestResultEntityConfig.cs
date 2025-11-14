// <copyright file="BacktestResultEntityConfig.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Backtest;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for BacktestResult entity.
/// </summary>
public class BacktestResultEntityConfig : IEntityTypeConfiguration<BacktestResult>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<BacktestResult> builder)
    {
        builder.ToTable("BacktestResults");

        builder.HasKey(x => x.BacktestId);

        builder.Property(x => x.BacktestId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.StrategyName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Symbol)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.StartDate)
            .IsRequired();

        builder.Property(x => x.EndDate)
            .IsRequired();

        builder.Property(x => x.InitialCapital)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.FinalEquity)
            .IsRequired()
            .HasPrecision(18, 2);

        // Ignore computed property
        builder.Ignore(x => x.TotalReturn);

        // Ignore computed property
        builder.Ignore(x => x.TotalPnL);

        builder.Property(x => x.SharpeRatio)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.MaxDrawdown)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.WinRate)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.ProfitFactor)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(x => x.TotalTrades)
            .IsRequired();

        // Ignore in-memory collections (not persisted to database)
        builder.Ignore(x => x.Trades);
        builder.Ignore(x => x.EquityCurve);
        builder.Ignore(x => x.Performance);

        // Use JSON serialized versions for database
        builder.Property(x => x.TradesJson)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(x => x.EquityCurveJson)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.Duration)
            .IsRequired();

        // Indexes for performance
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_BacktestResults_CreatedAt")
            .IsDescending();

        builder.HasIndex(x => new { x.StrategyName, x.Symbol })
            .HasDatabaseName("IX_BacktestResults_StrategySymbol");
    }
}
