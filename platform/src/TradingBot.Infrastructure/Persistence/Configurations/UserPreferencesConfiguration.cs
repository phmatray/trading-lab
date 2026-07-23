// <copyright file="UserPreferencesConfiguration.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Entities;
using TradingBot.Core.ValueObjects;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for UserPreferences entity.
/// </summary>
public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.UserId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Theme)
            .HasConversion(
                v => v.Name,
                v => (v == "Dark") ? Theme.Dark : Theme.Light)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.DashboardRefreshInterval)
            .IsRequired();

        builder.Property(p => p.NotificationDuration)
            .IsRequired();

        builder.Property(p => p.ShowSuccessNotifications)
            .IsRequired();

        builder.Property(p => p.ShowErrorNotifications)
            .IsRequired();

        builder.Property(p => p.ShowInfoNotifications)
            .IsRequired();

        builder.Property(p => p.ShowWarningNotifications)
            .IsRequired();

        builder.Property(p => p.CustomSettings)
            .HasColumnType("TEXT");

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();
    }
}
