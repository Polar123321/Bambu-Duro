using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services.Interfaces;

public interface IUserMemoryService
{
    bool ShouldTrackUser(ulong userId);

    Task CaptureMessageAsync(
        ulong guildId,
        ulong channelId,
        ulong userId,
        string username,
        string content,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default);

    Task<UserMemoryContext> GetContextAsync(
        ulong guildId,
        ulong userId,
        string? currentPrompt = null,
        int? maxMessages = null,
        CancellationToken cancellationToken = default);

    string BuildPromptContext(UserMemoryContext context);
}
