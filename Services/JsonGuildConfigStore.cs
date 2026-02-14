using System.Text.Json;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services;

public sealed class JsonGuildConfigStore : IGuildConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _basePath;

    public JsonGuildConfigStore()
    {
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "guild-config");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<GuildConfig> GetAsync(ulong guildId)
    {
        var path = GetFilePath(guildId);
        if (!File.Exists(path))
        {
            return new GuildConfig();
        }

        var json = await File.ReadAllTextAsync(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new GuildConfig();
        }

        return JsonSerializer.Deserialize<GuildConfig>(json, JsonOptions) ?? new GuildConfig();
    }

    public async Task SaveAsync(ulong guildId, GuildConfig config)
    {
        var path = GetFilePath(guildId);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    private string GetFilePath(ulong guildId)
    {
        return Path.Combine(_basePath, $"{guildId}.json");
    }
}
