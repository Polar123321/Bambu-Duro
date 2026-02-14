using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services.Interfaces;

public interface IModerationActionStore
{
    string Add(ModerationAction action, TimeSpan ttl);
    bool TryGet(string token, out ModerationAction action);
    void Remove(string token);
}
