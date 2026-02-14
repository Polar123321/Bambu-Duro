namespace ConsoleApp4.Services.Models;

public sealed class LongTermMemoryContext
{
    public int TotalMessagesStored { get; init; }
    public IReadOnlyList<string> Facts { get; init; } = Array.Empty<string>();
    public IReadOnlyList<UserMemoryMessageContext> RelevantMessages { get; init; } = Array.Empty<UserMemoryMessageContext>();

    public static LongTermMemoryContext Empty => new();
}
