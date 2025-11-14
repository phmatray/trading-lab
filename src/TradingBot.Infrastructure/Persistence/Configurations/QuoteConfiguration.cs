// <copyright file="QuoteConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.MarketData;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Quote.
/// </summary>
internal sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("quotes");

        // Composite key: Symbol + Timestamp
        builder.HasKey(q => new { q.Symbol, q.Timestamp });

        builder.Property(q => q.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(q => q.Timestamp)
            .HasColumnName("timestamp")
            .IsRequired();

        builder.Property(q => q.Price)
            .HasColumnName("price")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(q => q.Bid)
            .HasColumnName("bid")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(q => q.Ask)
            .HasColumnName("ask")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(q => q.Volume)
            .HasColumnName("volume")
            .IsRequired();

        builder.Property(q => q.Change)
            .HasColumnName("change")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(q => q.ChangePercent)
            .HasColumnName("change_percent")
            .HasPrecision(10, 4)
            .IsRequired();

        // Indexes
        builder.HasIndex(q => q.Symbol).HasDatabaseName("idx_quotes_symbol");
        builder.HasIndex(q => q.Timestamp).HasDatabaseName("idx_quotes_timestamp");
    }
}
