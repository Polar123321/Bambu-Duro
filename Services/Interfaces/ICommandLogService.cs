using ConsoleApp4.Models.Entities;

namespace ConsoleApp4.Services.Interfaces;

public interface ICommandLogService
{
    Task LogAsync(CommandLog logEntry);
}
