using Microsoft.EntityFrameworkCore;
using ConsoleApp4.Data;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Services;

public sealed class UserGuildStatsService : IUserGuildStatsService
{
    private readonly BotDbContext _db;

    public UserGuildStatsService(BotDbContext db)
    {
        _db = db;
    }

    public async Task IncrementMessagesAsync(ulong guildId, ulong userId)
    {
        var stats = await GetOrCreateAsync(guildId, userId);
        stats.MessageCount += 1;
        stats.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task IncrementMessagesAsync(ulong guildId, ulong channelId, ulong userId)
    {
        var now = DateTime.UtcNow;

        var guildStats = await GetOrCreateAsync(guildId, userId);
        guildStats.MessageCount += 1;
        guildStats.UpdatedAtUtc = now;

        var channelStats = await GetOrCreateChannelAsync(guildId, channelId, userId);
        channelStats.MessageCount += 1;
        channelStats.UpdatedAtUtc = now;

        await _db.SaveChangesAsync();
    }

    public async Task IncrementInvitesAsync(ulong guildId, ulong userId, int amount = 1)
    {
        if (amount <= 0)
        {
            return;
        }

        var stats = await GetOrCreateAsync(guildId, userId);
        stats.InviteCount += amount;
        stats.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<(int Messages, int Invites)> GetCountsAsync(ulong guildId, ulong userId)
    {
        var stats = await _db.UserGuildStats
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.DiscordGuildId == guildId && s.DiscordUserId == userId);

        if (stats == null)
        {
            return (0, 0);
        }

        return (stats.MessageCount, stats.InviteCount);
    }

    public async Task ReplaceMessageCountsAsync(ulong guildId, IReadOnlyDictionary<ulong, int> messageCounts)
    {
        var existing = await _db.UserGuildStats
            .Where(s => s.DiscordGuildId == guildId)
            .ToListAsync();

        var byUser = existing.ToDictionary(s => s.DiscordUserId);
        var now = DateTime.UtcNow;

        foreach (var entry in messageCounts)
        {
            if (byUser.TryGetValue(entry.Key, out var stats))
            {
                stats.MessageCount = entry.Value;
                stats.UpdatedAtUtc = now;
            }
            else
            {
                _db.UserGuildStats.Add(new UserGuildStats
                {
                    Id = Guid.NewGuid(),
                    DiscordGuildId = guildId,
                    DiscordUserId = entry.Key,
                    MessageCount = entry.Value,
                    InviteCount = 0,
                    UpdatedAtUtc = now
                });
            }
        }

        foreach (var stats in existing)
        {
            if (!messageCounts.ContainsKey(stats.DiscordUserId))
            {
                stats.MessageCount = 0;
                stats.UpdatedAtUtc = now;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task ReplaceChannelMessageCountsAsync(ulong guildId, ulong channelId, IReadOnlyDictionary<ulong, int> messageCounts, bool adjustGuildTotals)
    {
        var existingChannels = await _db.UserChannelStats
            .Where(s => s.DiscordGuildId == guildId && s.DiscordChannelId == channelId)
            .ToListAsync();

        var existingMap = existingChannels.ToDictionary(s => s.DiscordUserId);
        var now = DateTime.UtcNow;

        foreach (var entry in messageCounts)
        {
            var newCount = entry.Value;
            if (existingMap.TryGetValue(entry.Key, out var channelStats))
            {
                var delta = newCount - channelStats.MessageCount;
                channelStats.MessageCount = newCount;
                channelStats.UpdatedAtUtc = now;
                if (adjustGuildTotals && delta != 0)
                {
                    var guildStats = await GetOrCreateAsync(guildId, entry.Key);
                    guildStats.MessageCount += delta;
                    guildStats.UpdatedAtUtc = now;
                }
            }
            else
            {
                _db.UserChannelStats.Add(new UserChannelStats
                {
                    Id = Guid.NewGuid(),
                    DiscordGuildId = guildId,
                    DiscordChannelId = channelId,
                    DiscordUserId = entry.Key,
                    MessageCount = newCount,
                    UpdatedAtUtc = now
                });

                if (adjustGuildTotals && newCount != 0)
                {
                    var guildStats = await GetOrCreateAsync(guildId, entry.Key);
                    guildStats.MessageCount += newCount;
                    guildStats.UpdatedAtUtc = now;
                }
            }
        }

        foreach (var channelStats in existingChannels)
        {
            if (!messageCounts.ContainsKey(channelStats.DiscordUserId))
            {
                if (adjustGuildTotals && channelStats.MessageCount != 0)
                {
                    var guildStats = await GetOrCreateAsync(guildId, channelStats.DiscordUserId);
                    guildStats.MessageCount -= channelStats.MessageCount;
                    guildStats.UpdatedAtUtc = now;
                }

                channelStats.MessageCount = 0;
                channelStats.UpdatedAtUtc = now;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task ReplaceUserChannelMessageCountAsync(ulong guildId, ulong channelId, ulong userId, int messageCount, bool adjustGuildTotals)
    {
        if (messageCount < 0)
        {
            messageCount = 0;
        }

        var now = DateTime.UtcNow;
        var channelStats = await _db.UserChannelStats
            .FirstOrDefaultAsync(s =>
                s.DiscordGuildId == guildId &&
                s.DiscordChannelId == channelId &&
                s.DiscordUserId == userId);

        if (channelStats != null)
        {
            var delta = messageCount - channelStats.MessageCount;
            channelStats.MessageCount = messageCount;
            channelStats.UpdatedAtUtc = now;

            if (adjustGuildTotals && delta != 0)
            {
                var guildStats = await GetOrCreateAsync(guildId, userId);
                guildStats.MessageCount += delta;
                guildStats.UpdatedAtUtc = now;
            }
        }
        else
        {
            _db.UserChannelStats.Add(new UserChannelStats
            {
                Id = Guid.NewGuid(),
                DiscordGuildId = guildId,
                DiscordChannelId = channelId,
                DiscordUserId = userId,
                MessageCount = messageCount,
                UpdatedAtUtc = now
            });

            if (adjustGuildTotals && messageCount != 0)
            {
                var guildStats = await GetOrCreateAsync(guildId, userId);
                guildStats.MessageCount += messageCount;
                guildStats.UpdatedAtUtc = now;
            }
        }

        await _db.SaveChangesAsync();
    }

    private async Task<UserGuildStats> GetOrCreateAsync(ulong guildId, ulong userId)
    {
        var stats = await _db.UserGuildStats
            .FirstOrDefaultAsync(s => s.DiscordGuildId == guildId && s.DiscordUserId == userId);

        if (stats != null)
        {
            return stats;
        }

        stats = new UserGuildStats
        {
            Id = Guid.NewGuid(),
            DiscordGuildId = guildId,
            DiscordUserId = userId,
            MessageCount = 0,
            InviteCount = 0,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.UserGuildStats.Add(stats);

        try
        {
            await _db.SaveChangesAsync();
            return stats;
        }
        catch (DbUpdateException)
        {
            var existing = await _db.UserGuildStats
                .FirstOrDefaultAsync(s => s.DiscordGuildId == guildId && s.DiscordUserId == userId);
            if (existing != null)
            {
                return existing;
            }

            throw;
        }
    }

    private async Task<UserChannelStats> GetOrCreateChannelAsync(ulong guildId, ulong channelId, ulong userId)
    {
        var stats = await _db.UserChannelStats
            .FirstOrDefaultAsync(s =>
                s.DiscordGuildId == guildId &&
                s.DiscordChannelId == channelId &&
                s.DiscordUserId == userId);

        if (stats != null)
        {
            return stats;
        }

        stats = new UserChannelStats
        {
            Id = Guid.NewGuid(),
            DiscordGuildId = guildId,
            DiscordChannelId = channelId,
            DiscordUserId = userId,
            MessageCount = 0,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.UserChannelStats.Add(stats);

        try
        {
            await _db.SaveChangesAsync();
            return stats;
        }
        catch (DbUpdateException)
        {
            var existing = await _db.UserChannelStats
                .FirstOrDefaultAsync(s =>
                    s.DiscordGuildId == guildId &&
                    s.DiscordChannelId == channelId &&
                    s.DiscordUserId == userId);
            if (existing != null)
            {
                return existing;
            }

            throw;
        }
    }
}
