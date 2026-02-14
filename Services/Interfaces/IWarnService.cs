using ConsoleApp4.Models.Entities;

namespace ConsoleApp4.Services.Interfaces;

public interface IWarnService
{
    Task<WarnEntry> AddWarnAsync(ulong guildId, ulong userId, ulong moderatorId, string reason, DateTime createdAtUtc);
    Task<IReadOnlyList<(ulong UserId, int ActiveCount, DateTime LastAtUtc)>> GetWarnedUsersAsync(ulong guildId);
    Task<IReadOnlyList<WarnEntry>> GetAllActiveWarnsAsync(ulong guildId);
    Task<IReadOnlyList<WarnEntry>> GetActiveWarnsAsync(ulong guildId, ulong userId);
    Task<int> RevokeAllAsync(ulong guildId, ulong userId, ulong revokedById);
    Task<bool> RevokeAsync(ulong guildId, Guid warnId, ulong revokedById);
}
