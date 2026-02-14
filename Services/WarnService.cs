using Microsoft.EntityFrameworkCore;
using ConsoleApp4.Data;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Services;

public sealed class WarnService : IWarnService
{
    private readonly BotDbContext _db;

    public WarnService(BotDbContext db)
    {
        _db = db;
    }

    public async Task<WarnEntry> AddWarnAsync(ulong guildId, ulong userId, ulong moderatorId, string reason, DateTime createdAtUtc)
    {
        var warn = new WarnEntry
        {
            Id = Guid.NewGuid(),
            DiscordGuildId = guildId,
            DiscordUserId = userId,
            DiscordModeratorId = moderatorId,
            Reason = (reason ?? string.Empty).Trim(),
            CreatedAtUtc = createdAtUtc
        };

        _db.WarnEntries.Add(warn);
        await _db.SaveChangesAsync();
        return warn;
    }

    public async Task<IReadOnlyList<(ulong UserId, int ActiveCount, DateTime LastAtUtc)>> GetWarnedUsersAsync(ulong guildId)
    {
        var list = await _db.WarnEntries
            .AsNoTracking()
            .Where(w => w.DiscordGuildId == guildId && w.RevokedAtUtc == null)
            .GroupBy(w => w.DiscordUserId)
            .Select(g => new
            {
                UserId = g.Key,
                ActiveCount = g.Count(),
                LastAtUtc = g.Max(x => x.CreatedAtUtc)
            })
            .OrderByDescending(x => x.ActiveCount)
            .ThenByDescending(x => x.LastAtUtc)
            .ToListAsync();

        return list.Select(x => (x.UserId, x.ActiveCount, x.LastAtUtc)).ToList();
    }

    public async Task<IReadOnlyList<WarnEntry>> GetAllActiveWarnsAsync(ulong guildId)
    {
        return await _db.WarnEntries
            .AsNoTracking()
            .Where(w => w.DiscordGuildId == guildId && w.RevokedAtUtc == null)
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<WarnEntry>> GetActiveWarnsAsync(ulong guildId, ulong userId)
    {
        return await _db.WarnEntries
            .AsNoTracking()
            .Where(w => w.DiscordGuildId == guildId && w.DiscordUserId == userId && w.RevokedAtUtc == null)
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToListAsync();
    }

    public async Task<int> RevokeAllAsync(ulong guildId, ulong userId, ulong revokedById)
    {
        var now = DateTime.UtcNow;
        return await _db.WarnEntries
            .Where(w => w.DiscordGuildId == guildId && w.DiscordUserId == userId && w.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.RevokedAtUtc, now)
                .SetProperty(w => w.RevokedById, revokedById));
    }

    public async Task<bool> RevokeAsync(ulong guildId, Guid warnId, ulong revokedById)
    {
        var warn = await _db.WarnEntries
            .FirstOrDefaultAsync(w => w.DiscordGuildId == guildId && w.Id == warnId);

        if (warn == null || warn.RevokedAtUtc != null)
        {
            return false;
        }

        warn.RevokedAtUtc = DateTime.UtcNow;
        warn.RevokedById = revokedById;
        await _db.SaveChangesAsync();
        return true;
    }
}
