using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services.Interfaces;

public interface IStaffApplicationStore
{
    Task AddAsync(StaffApplication application);
    Task<IReadOnlyList<StaffApplication>> GetByUserAsync(ulong guildId, ulong userId);
    Task<IReadOnlyList<StaffApplication>> GetAllAsync(ulong guildId);
    Task<StaffApplication?> GetByIdAsync(ulong guildId, Guid applicationId);
    Task UpdateStatusAsync(ulong guildId, Guid applicationId, string status);
    Task SetMessageIdAsync(ulong guildId, Guid applicationId, ulong messageId);
    Task SetChannelAsync(ulong guildId, ulong channelId);
    Task<ulong> GetChannelAsync(ulong guildId);
    Task<bool> IsBannedAsync(ulong guildId, ulong userId);
    Task BanUserAsync(ulong guildId, ulong userId);
    Task<IReadOnlyList<ulong>> GetRolesAsync(ulong guildId);
    Task AddRoleAsync(ulong guildId, ulong roleId);
    Task SetRolesAsync(ulong guildId, IReadOnlyList<ulong> roleIds);
    Task<IReadOnlyList<string>> GetGlobalQuestionsAsync(ulong guildId);
    Task<IReadOnlyList<string>> GetRoleQuestionsAsync(ulong guildId, ulong roleId);
    Task<IReadOnlyList<string>> GetQuestionsForRoleAsync(ulong guildId, ulong? roleId);
    Task AddGlobalQuestionAsync(ulong guildId, string question);
    Task AddRoleQuestionAsync(ulong guildId, ulong roleId, string question);
    Task RemoveGlobalQuestionAsync(ulong guildId, int index);
    Task RemoveRoleQuestionAsync(ulong guildId, ulong roleId, int index);
}
