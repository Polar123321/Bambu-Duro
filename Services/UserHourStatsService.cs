using Microsoft.EntityFrameworkCore;
using ConsoleApp4.Data;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Services;

public sealed class UserHourStatsService : IUserHourStatsService
{
    private readonly BotDbContext _db;

    public UserHourStatsService(BotDbContext db)
    {
        _db = db;
    }

    public async Task IncrementMessageAsync(ulong guildId, ulong userId, DateTime utcNow)
    {
        
        var local = utcNow.ToLocalTime();
        var hourOfWeek = ((int)local.DayOfWeek * 24) + local.Hour; 

        var stats = await _db.UserHourStats
            .FirstOrDefaultAsync(s =>
                s.DiscordGuildId == guildId &&
                s.DiscordUserId == userId &&
                s.HourOfWeek == hourOfWeek);

        if (stats != null)
        {
            stats.MessageCount += 1;
            stats.UpdatedAtUtc = utcNow;
        }
        else
        {
            _db.UserHourStats.Add(new UserHourStats
            {
                Id = Guid.NewGuid(),
                DiscordGuildId = guildId,
                DiscordUserId = userId,
                HourOfWeek = hourOfWeek,
                MessageCount = 1,
                UpdatedAtUtc = utcNow
            });
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            
            var existing = await _db.UserHourStats
                .FirstOrDefaultAsync(s =>
                    s.DiscordGuildId == guildId &&
                    s.DiscordUserId == userId &&
                    s.HourOfWeek == hourOfWeek);
            if (existing != null)
            {
                existing.MessageCount += 1;
                existing.UpdatedAtUtc = utcNow;
                await _db.SaveChangesAsync();
            }
        }
    }

    public async Task<IReadOnlyDictionary<int, int>> GetHourOfWeekCountsAsync(ulong guildId, ulong userId)
    {
        var rows = await _db.UserHourStats
            .AsNoTracking()
            .Where(s => s.DiscordGuildId == guildId && s.DiscordUserId == userId && s.MessageCount > 0)
            .Select(s => new { s.HourOfWeek, s.MessageCount })
            .ToListAsync();

        return rows.ToDictionary(r => r.HourOfWeek, r => r.MessageCount);
    }
}

