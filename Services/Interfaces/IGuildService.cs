using ConsoleApp4.Models.Entities;

namespace ConsoleApp4.Services.Interfaces;

public interface IGuildService
{
    Task<Guild> GetOrCreateAsync(ulong guildId, string name);
}
