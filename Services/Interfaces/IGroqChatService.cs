namespace ConsoleApp4.Services.Interfaces;

public interface IGroqChatService
{
    Task<string> WhatIfAsync(string scenario, string userName, CancellationToken cancellationToken = default);
    Task<string> MentionReplyAsync(
        string prompt,
        string userName,
        string? guildName = null,
        string? userMemoryContext = null,
        CancellationToken cancellationToken = default);
}
