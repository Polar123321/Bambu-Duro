namespace ConsoleApp4.Services.Interfaces;

public interface IRateLimitService
{
    bool TryConsume(ulong userId, string bucket, TimeSpan window);
    bool TryConsume(ulong userId, string bucket, TimeSpan window, out TimeSpan retryAfter);
}
