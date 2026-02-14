namespace ConsoleApp4.Services.Interfaces;

public interface IUserGuildStatsService
{
    Task IncrementMessagesAsync(ulong guildId, ulong userId);
    Task IncrementMessagesAsync(ulong guildId, ulong channelId, ulong userId);
    Task IncrementInvitesAsync(ulong guildId, ulong userId, int amount = 1);
    Task<(int Messages, int Invites)> GetCountsAsync(ulong guildId, ulong userId);
    Task ReplaceMessageCountsAsync(ulong guildId, IReadOnlyDictionary<ulong, int> messageCounts);
    Task ReplaceChannelMessageCountsAsync(ulong guildId, ulong channelId, IReadOnlyDictionary<ulong, int> messageCounts, bool adjustGuildTotals);
    Task ReplaceUserChannelMessageCountAsync(ulong guildId, ulong channelId, ulong userId, int messageCount, bool adjustGuildTotals);
}
