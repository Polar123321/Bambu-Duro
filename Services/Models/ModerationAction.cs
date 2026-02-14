namespace ConsoleApp4.Services.Models;

public sealed record ModerationAction(
    string Token,
    ModerationActionType Type,
    ulong GuildId,
    ulong TargetUserId,
    ulong RequestedById,
    ulong ChannelId,
    int Amount,
    int DurationMinutes,
    string Reason,
    DateTime CreatedAtUtc);
