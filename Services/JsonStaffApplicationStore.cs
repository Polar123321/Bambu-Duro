using System.Text.Json;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Services;

public sealed class JsonStaffApplicationStore : IStaffApplicationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _basePath;

    public JsonStaffApplicationStore()
    {
        _basePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "staff-applications");
        Directory.CreateDirectory(_basePath);
    }

    public async Task AddAsync(StaffApplication application)
    {
        var list = await LoadAsync(application.GuildId);
        list.Add(application);
        await SaveAsync(application.GuildId, list);
    }

    public async Task<IReadOnlyList<StaffApplication>> GetByUserAsync(ulong guildId, ulong userId)
    {
        var list = await LoadAsync(guildId);
        return list.Where(a => a.UserId == userId)
            .OrderByDescending(a => a.SubmittedAtUtc)
            .ToList();
    }

    public async Task<IReadOnlyList<StaffApplication>> GetAllAsync(ulong guildId)
    {
        var list = await LoadAsync(guildId);
        return list.OrderByDescending(a => a.SubmittedAtUtc).ToList();
    }

    public async Task<StaffApplication?> GetByIdAsync(ulong guildId, Guid applicationId)
    {
        var list = await LoadAsync(guildId);
        return list.FirstOrDefault(a => a.ApplicationId == applicationId);
    }

    public async Task UpdateStatusAsync(ulong guildId, Guid applicationId, string status)
    {
        var list = await LoadAsync(guildId);
        var index = list.FindIndex(a => a.ApplicationId == applicationId);
        if (index < 0)
        {
            return;
        }

        var current = list[index];
        list[index] = current with
        {
            Status = status,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await SaveAsync(guildId, list);
    }

    public async Task SetMessageIdAsync(ulong guildId, Guid applicationId, ulong messageId)
    {
        var list = await LoadAsync(guildId);
        var index = list.FindIndex(a => a.ApplicationId == applicationId);
        if (index < 0)
        {
            return;
        }

        var current = list[index];
        list[index] = current with { MessageId = messageId };
        await SaveAsync(guildId, list);
    }

    public async Task SetChannelAsync(ulong guildId, ulong channelId)
    {
        var config = await LoadConfigAsync(guildId);
        config.ChannelId = channelId;
        await SaveConfigAsync(guildId, config);
    }

    public async Task<ulong> GetChannelAsync(ulong guildId)
    {
        var config = await LoadConfigAsync(guildId);
        return config.ChannelId;
    }

    public async Task<bool> IsBannedAsync(ulong guildId, ulong userId)
    {
        var config = await LoadConfigAsync(guildId);
        return config.BannedUserIds.Contains(userId);
    }

    public async Task BanUserAsync(ulong guildId, ulong userId)
    {
        var config = await LoadConfigAsync(guildId);
        if (!config.BannedUserIds.Contains(userId))
        {
            config.BannedUserIds.Add(userId);
            await SaveConfigAsync(guildId, config);
        }
    }

    public async Task<IReadOnlyList<ulong>> GetRolesAsync(ulong guildId)
    {
        var config = await LoadConfigAsync(guildId);
        return config.RoleIds;
    }

    public async Task AddRoleAsync(ulong guildId, ulong roleId)
    {
        var config = await LoadConfigAsync(guildId);
        if (!config.RoleIds.Contains(roleId))
        {
            config.RoleIds.Add(roleId);
            await SaveConfigAsync(guildId, config);
        }
    }

    public async Task SetRolesAsync(ulong guildId, IReadOnlyList<ulong> roleIds)
    {
        var config = await LoadConfigAsync(guildId);
        config.RoleIds = roleIds.ToList();
        await SaveConfigAsync(guildId, config);
    }

    public async Task<IReadOnlyList<string>> GetGlobalQuestionsAsync(ulong guildId)
    {
        var config = await LoadConfigAsync(guildId);
        return config.GlobalQuestions;
    }

    public async Task<IReadOnlyList<string>> GetRoleQuestionsAsync(ulong guildId, ulong roleId)
    {
        var config = await LoadConfigAsync(guildId);
        return config.RoleQuestions.TryGetValue(roleId, out var questions)
            ? questions
            : Array.Empty<string>();
    }

    public async Task<IReadOnlyList<string>> GetQuestionsForRoleAsync(ulong guildId, ulong? roleId)
    {
        var config = await LoadConfigAsync(guildId);
        var list = new List<string>(config.GlobalQuestions);

        if (roleId.HasValue &&
            config.RoleQuestions.TryGetValue(roleId.Value, out var roleQuestions))
        {
            list.AddRange(roleQuestions);
        }

        return list;
    }

    public async Task AddGlobalQuestionAsync(ulong guildId, string question)
    {
        var text = question.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var config = await LoadConfigAsync(guildId);
        if (!config.GlobalQuestions.Any(q => q.Equals(text, StringComparison.OrdinalIgnoreCase)))
        {
            config.GlobalQuestions.Add(text);
            await SaveConfigAsync(guildId, config);
        }
    }

    public async Task AddRoleQuestionAsync(ulong guildId, ulong roleId, string question)
    {
        var text = question.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var config = await LoadConfigAsync(guildId);
        if (!config.RoleQuestions.TryGetValue(roleId, out var questions))
        {
            questions = new List<string>();
            config.RoleQuestions[roleId] = questions;
        }

        if (!questions.Any(q => q.Equals(text, StringComparison.OrdinalIgnoreCase)))
        {
            questions.Add(text);
            await SaveConfigAsync(guildId, config);
        }
    }

    public async Task RemoveGlobalQuestionAsync(ulong guildId, int index)
    {
        var config = await LoadConfigAsync(guildId);
        if (index < 0 || index >= config.GlobalQuestions.Count)
        {
            return;
        }

        config.GlobalQuestions.RemoveAt(index);
        await SaveConfigAsync(guildId, config);
    }

    public async Task RemoveRoleQuestionAsync(ulong guildId, ulong roleId, int index)
    {
        var config = await LoadConfigAsync(guildId);
        if (!config.RoleQuestions.TryGetValue(roleId, out var questions))
        {
            return;
        }

        if (index < 0 || index >= questions.Count)
        {
            return;
        }

        questions.RemoveAt(index);
        if (questions.Count == 0)
        {
            config.RoleQuestions.Remove(roleId);
        }

        await SaveConfigAsync(guildId, config);
    }

    private string GetFilePath(ulong guildId)
    {
        return Path.Combine(_basePath, $"{guildId}.json");
    }

    private string GetConfigPath(ulong guildId)
    {
        return Path.Combine(_basePath, $"{guildId}-config.json");
    }

    private async Task<List<StaffApplication>> LoadAsync(ulong guildId)
    {
        var path = GetFilePath(guildId);
        if (!File.Exists(path))
        {
            return new List<StaffApplication>();
        }

        var json = await File.ReadAllTextAsync(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<StaffApplication>();
        }

        return JsonSerializer.Deserialize<List<StaffApplication>>(json, JsonOptions) ?? new List<StaffApplication>();
    }

    private async Task SaveAsync(ulong guildId, List<StaffApplication> applications)
    {
        var path = GetFilePath(guildId);
        var json = JsonSerializer.Serialize(applications, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    private async Task<StaffApplicationConfig> LoadConfigAsync(ulong guildId)
    {
        var path = GetConfigPath(guildId);
        if (!File.Exists(path))
        {
            return new StaffApplicationConfig();
        }

        var json = await File.ReadAllTextAsync(path);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new StaffApplicationConfig();
        }

        var config = JsonSerializer.Deserialize<StaffApplicationConfig>(json, JsonOptions) ?? new StaffApplicationConfig();
        config.GlobalQuestions ??= new List<string>();
        config.RoleQuestions ??= new Dictionary<ulong, List<string>>();
        return config;
    }

    private async Task SaveConfigAsync(ulong guildId, StaffApplicationConfig config)
    {
        var path = GetConfigPath(guildId);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        await File.WriteAllTextAsync(path, json);
    }
}
