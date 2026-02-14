namespace ConsoleApp4.Services.Models;

public sealed record MarriageRecord(
    ulong UserId1,
    ulong UserId2,
    DateTime MarriedAtUtc);

public sealed class MarriageData
{
    public List<MarriageRecord> Marriages { get; set; } = new();
}
