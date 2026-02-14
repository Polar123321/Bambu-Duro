using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services.Interfaces;

public interface IGuildConfigStore
{
    Task<GuildConfig> GetAsync(ulong guildId);
    Task SaveAsync(ulong guildId, GuildConfig config);
}
