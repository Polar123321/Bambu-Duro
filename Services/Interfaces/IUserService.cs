using ConsoleApp4.Models.Entities;

namespace ConsoleApp4.Services.Interfaces;

public interface IUserService
{
    Task<User> GetOrCreateAsync(ulong userId, string username);
}
