namespace ConsoleApp4.Services.Models;

public sealed record ShipRecord(
    ulong UserId1,
    ulong UserId2,
    int Compatibility,
    DateTime CreatedAtUtc,
    bool IsManual = false);

public sealed class ShipData
{
    public List<ShipRecord> Ships { get; set; } = new();
}
