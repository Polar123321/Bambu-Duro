namespace ConsoleApp4.Services.Interfaces;

public interface IUserHourStatsService
{
    Task IncrementMessageAsync(ulong guildId, ulong userId, DateTime utcNow);
    Task<IReadOnlyDictionary<int, int>> GetHourOfWeekCountsAsync(ulong guildId, ulong userId);
}

