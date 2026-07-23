// <copyright file="AccountConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Portfolio;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Account.
/// </summary>
internal sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.AccountId);

        // Ignore domain events collection (not persisted)
        builder.Ignore(a => a.DomainEvents);

        builder.Property(a => a.AccountId)
            .HasColumnName("account_id")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.Equity)
            .HasColumnName("equity")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.Cash)
            .HasColumnName("cash")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.PositionValue)
            .HasColumnName("position_value")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(a => a.BuyingPower)
            .HasColumnName("buying_power")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(a => a.Leverage)
            .HasColumnName("leverage")
            .HasPrecision(10, 2)
            .HasDefaultValue(1m)
            .IsRequired();

        builder.Property(a => a.UnrealizedPnL)
            .HasColumnName("unrealized_pnl")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(a => a.RealizedPnL)
            .HasColumnName("realized_pnl")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m)
            .IsRequired();
    }
}
