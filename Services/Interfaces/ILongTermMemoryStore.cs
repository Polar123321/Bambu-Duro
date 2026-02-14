using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services.Interfaces;

public interface ILongTermMemoryStore
{
    Task AppendMessageAsync(
        ulong guildId,
        ulong channelId,
        ulong userId,
        string username,
        string content,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default);

    Task<LongTermMemoryContext> GetContextAsync(
        ulong guildId,
        ulong userId,
        string? currentPrompt = null,
        int? maxMessages = null,
        CancellationToken cancellationToken = default);
}
