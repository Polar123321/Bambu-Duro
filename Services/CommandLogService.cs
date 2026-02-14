using ConsoleApp4.Data;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Services;

public sealed class CommandLogService : ICommandLogService
{
    private readonly BotDbContext _db;

    public CommandLogService(BotDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(CommandLog logEntry)
    {
        _db.CommandLogs.Add(logEntry);
        await _db.SaveChangesAsync();
    }
}
