using System.Text.Json;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services;

public sealed class JsonMarriageStore : IMarriageStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public JsonMarriageStore()
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "data");
        Directory.CreateDirectory(basePath);
        _filePath = Path.Combine(basePath, "marriages.json");
    }

    public async Task<IReadOnlyList<MarriageRecord>> GetAsync(ulong guildId)
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<MarriageRecord>();
        }

        var json = await File.ReadAllTextAsync(_filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<MarriageRecord>();
        }

        var data = JsonSerializer.Deserialize<MarriageData>(json, JsonOptions) ?? new MarriageData();
        return data.Marriages;
    }

    public async Task SaveAsync(ulong guildId, IReadOnlyList<MarriageRecord> records)
    {
        var data = new MarriageData { Marriages = records.ToList() };
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json);
    }
}
