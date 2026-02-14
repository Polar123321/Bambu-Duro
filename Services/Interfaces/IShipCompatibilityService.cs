using Discord.WebSocket;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services.Interfaces;

public interface IShipCompatibilityService
{
    Task<ShipCompatibilityResult> CalculateAsync(SocketGuild guild, ulong userId1, ulong userId2, CancellationToken cancellationToken = default);
}

