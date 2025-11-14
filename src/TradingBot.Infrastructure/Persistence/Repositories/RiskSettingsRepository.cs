// <copyright file="RiskSettingsRepository.cs" company="TradingBot">
// Copyright (c) TradingBot. All rights reserved.
// </copyright>

namespace TradingBot.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using TradingBot.Core.Interfaces;
using TradingBot.Core.Models.Configuration;

/// <summary>
/// Repository implementation for risk settings persistence.
/// </summary>
public class RiskSettingsRepository : IRiskSettingsRepository
{
    private readonly TradingBotDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskSettingsRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public RiskSettingsRepository(TradingBotDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<RiskSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RiskSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(RiskSettings settings, CancellationToken cancellationToken = default)
    {
        var existing = await _context.RiskSettings.FirstOrDefaultAsync(cancellationToken);

        if (existing == null)
        {
            // Create new record with fixed singleton ID
            settings.Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            settings.CreatedAt = DateTime.UtcNow;
            settings.LastModified = DateTime.UtcNow;
            _context.RiskSettings.Add(settings);
        }
        else
        {
            // Update existing record
            existing.MaxPositionSizePercent = settings.MaxPositionSizePercent;
            existing.StopLossPercent = settings.StopLossPercent;
            existing.TakeProfitPercent = settings.TakeProfitPercent;
            existing.MaxOpenPositions = settings.MaxOpenPositions;
            existing.MaxDailyLossPercent = settings.MaxDailyLossPercent;
            existing.LastModified = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
