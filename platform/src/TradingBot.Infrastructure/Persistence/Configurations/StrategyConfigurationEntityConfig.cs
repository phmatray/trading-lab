// <copyright file="StrategyConfigurationEntityConfig.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradingBot.Core.Models.Configuration;

namespace TradingBot.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for StrategyConfiguration entity.
/// </summary>
public class StrategyConfigurationEntityConfig : IEntityTypeConfiguration<StrategyConfiguration>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<StrategyConfiguration> builder)
    {
        builder.ToTable("StrategyConfigurations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StrategyName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(x => x.StrategyName)
            .IsUnique();

        builder.Property(x => x.ParametersJson)
            .IsRequired()
            .HasDefaultValue("{}");

        builder.Property(x => x.LastModified)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();
    }
}
