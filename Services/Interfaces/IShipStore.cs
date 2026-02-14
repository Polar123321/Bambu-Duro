using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services.Interfaces;

public interface IShipStore
{
    Task<ShipRecord?> GetAsync(ulong userId1, ulong userId2);
    Task SaveAsync(ShipRecord record);
}
