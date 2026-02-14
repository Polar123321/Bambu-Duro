using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Configuration;

public sealed class BrainConfiguration
{
    public bool Enabled { get; init; } = true;

    public bool EnableMoralReasoning { get; init; } = false;

    [Range(10, 500)]
    public int MaxMessagesPerUser { get; init; } = 120;

    [Range(1, 24)]
    public int MaxContextMessages { get; init; } = 16;

    [Range(1, 365)]
    public int RetentionDays { get; init; } = 120;

    public bool LongJsonMemoryEnabled { get; init; } = true;

    [Range(500, 200000)]
    public int LongJsonMaxMessagesPerUser { get; init; } = 50000;

    [Range(4, 200)]
    public int LongJsonContextMessages { get; init; } = 24;

    /// <summary>
    /// Directory for long-term JSON memories. Relative paths are stored under LocalApplicationData/ConsoleApp4.
    /// </summary>
    public string LongJsonDirectory { get; init; } = "long-memory";

    /// <summary>
    /// If empty, tracks every user. Otherwise, tracks only these Discord user IDs.
    /// </summary>
    public List<ulong> TrackedUserIds { get; init; } = new();
}
