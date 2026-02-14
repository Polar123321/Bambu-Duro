using System.Text.Json;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services;

public sealed class JsonShipStore : IShipStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public JsonShipStore()
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        Directory.CreateDirectory(basePath);
        _filePath = Path.Combine(basePath, "ships.json");
    }

    public async Task<ShipRecord?> GetAsync(ulong userId1, ulong userId2)
    {
        var data = await ReadAsync();
        return data.Ships.FirstOrDefault(s => s.UserId1 == userId1 && s.UserId2 == userId2);
    }

    public async Task SaveAsync(ShipRecord record)
    {
        var data = await ReadAsync();
        data.Ships.RemoveAll(s => s.UserId1 == record.UserId1 && s.UserId2 == record.UserId2);
        data.Ships.Add(record);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }

    private async Task<ShipData> ReadAsync()
    {
        if (!File.Exists(_filePath))
        {
            return new ShipData();
        }

        var json = await File.ReadAllTextAsync(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new ShipData();
        }

        return JsonSerializer.Deserialize<ShipData>(json, JsonOptions) ?? new ShipData();
    }
}
