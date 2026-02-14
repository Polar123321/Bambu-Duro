using Microsoft.EntityFrameworkCore;
using ConsoleApp4.Data;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Services;

public sealed class GuildService : IGuildService
{
    private readonly BotDbContext _db;

    public GuildService(BotDbContext db)
    {
        _db = db;
    }

    public async Task<Guild> GetOrCreateAsync(ulong guildId, string name)
    {
        var existing = await _db.Guilds.FirstOrDefaultAsync(g => g.DiscordGuildId == guildId);
        if (existing != null)
        {
            existing.Name = name;
            await _db.SaveChangesAsync();
            return existing;
        }

        var guild = new Guild
        {
            Id = Guid.NewGuid(),
            DiscordGuildId = guildId,
            Name = name
        };

        _db.Guilds.Add(guild);
        await _db.SaveChangesAsync();
        return guild;
    }
}
