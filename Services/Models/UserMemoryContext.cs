namespace ConsoleApp4.Services.Models;

public sealed class UserMemoryMessageContext
{
    public DateTime CreatedAtUtc { get; init; }
    public string Content { get; init; } = string.Empty;
    public string MoralTag { get; init; } = "neutro";
}

public sealed class UserMemoryContext
{
    public ulong DiscordGuildId { get; init; }
    public ulong DiscordUserId { get; init; }
    public int TotalMessagesStored { get; init; }
    public int PositiveCount { get; init; }
    public int NeutralCount { get; init; }
    public int QuestionableCount { get; init; }
    public int LongJsonMessagesStored { get; init; }
    public IReadOnlyList<string> UserFacts { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> LongJsonFacts { get; init; } = Array.Empty<string>();
    public IReadOnlyList<UserMemoryMessageContext> RecentMessages { get; init; } = Array.Empty<UserMemoryMessageContext>();
    public IReadOnlyList<UserMemoryMessageContext> LongJsonRelevantMessages { get; init; } = Array.Empty<UserMemoryMessageContext>();
    public string MoralSummary { get; init; } = "Sem historico.";

    public static UserMemoryContext Empty(ulong guildId, ulong userId)
    {
        return new UserMemoryContext
        {
            DiscordGuildId = guildId,
            DiscordUserId = userId,
            MoralSummary = "Sem historico salvo para este usuario."
        };
    }
}
