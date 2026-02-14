namespace ConsoleApp4.Services.Models;

public sealed record StaffApplication(
    Guid ApplicationId,
    ulong GuildId,
    ulong UserId,
    string Username,
    DateTime SubmittedAtUtc,
    string Motivation,
    string Experience,
    string Availability,
    string Status,
    DateTime? UpdatedAtUtc,
    ulong? MessageId,
    ulong? RoleId,
    string? RoleName,
    IReadOnlyList<string>? ExtraQuestions = null,
    string? ExtraAnswers = null);
