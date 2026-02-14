using Microsoft.EntityFrameworkCore;
using ConsoleApp4.Data;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Services;

public sealed class UserService : IUserService
{
    private readonly BotDbContext _db;

    public UserService(BotDbContext db)
    {
        _db = db;
    }

    public async Task<User> GetOrCreateAsync(ulong userId, string username)
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.DiscordUserId == userId);
        if (existing != null)
        {
            existing.Username = username;
            await _db.SaveChangesAsync();
            return existing;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            DiscordUserId = userId,
            Username = username
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }
}
