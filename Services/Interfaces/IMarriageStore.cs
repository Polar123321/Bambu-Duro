using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services.Interfaces;

public interface IMarriageStore
{
    Task<IReadOnlyList<MarriageRecord>> GetAsync(ulong guildId);
    Task SaveAsync(ulong guildId, IReadOnlyList<MarriageRecord> records);
}
