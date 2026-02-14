using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Handlers;

public sealed class InviteTrackingHandler
{
    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly ILogger<InviteTrackingHandler> _logger;
    private readonly ConcurrentDictionary<ulong, Dictionary<string, InviteSnapshot>> _cache = new();

    public InviteTrackingHandler(
        DiscordSocketClient client,
        IServiceProvider services,
        ILogger<InviteTrackingHandler> logger)
    {
        _client = client;
        _services = services;
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        _client.Ready += OnReadyAsync;
        _client.GuildAvailable += OnGuildAvailableAsync;
        _client.InviteCreated += OnInviteCreatedAsync;
        _client.InviteDeleted += OnInviteDeletedAsync;
        _client.UserJoined += OnUserJoinedAsync;
        return Task.CompletedTask;
    }

    private async Task OnReadyAsync()
    {
        foreach (var guild in _client.Guilds)
        {
            await CacheGuildInvitesAsync(guild);
        }
    }

    private Task OnGuildAvailableAsync(SocketGuild guild)
    {
        return CacheGuildInvitesAsync(guild);
    }

    private Task OnInviteCreatedAsync(SocketInvite invite)
    {
        if (invite.GuildId == null)
        {
            return Task.CompletedTask;
        }

        var guildId = invite.GuildId.Value;
        var map = _cache.GetOrAdd(guildId, _ => new Dictionary<string, InviteSnapshot>());
        map[invite.Code] = new InviteSnapshot(invite.Uses, invite.Inviter?.Id);
        return Task.CompletedTask;
    }

    private Task OnInviteDeletedAsync(SocketGuildChannel channel, string code)
    {
        if (_cache.TryGetValue(channel.Guild.Id, out var map))
        {
            map.Remove(code);
        }

        return Task.CompletedTask;
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        try
        {
            var inviterId = await TryResolveInviterAsync(user.Guild);
            if (!inviterId.HasValue)
            {
                return;
            }

            using var scope = _services.CreateScope();
            var stats = scope.ServiceProvider.GetRequiredService<IUserGuildStatsService>();
            await stats.IncrementInvitesAsync(user.Guild.Id, inviterId.Value, 1);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve inviter for guild {GuildId}", user.Guild.Id);
        }
    }

    private async Task CacheGuildInvitesAsync(SocketGuild guild)
    {
        try
        {
            var invites = await guild.GetInvitesAsync();
            var map = invites.ToDictionary(
                invite => invite.Code,
                invite => new InviteSnapshot(invite.Uses ?? 0, invite.Inviter?.Id));
            _cache[guild.Id] = map;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unable to cache invites for guild {GuildId}", guild.Id);
        }
    }

    private async Task<ulong?> TryResolveInviterAsync(SocketGuild guild)
    {
        IReadOnlyCollection<IInviteMetadata> invites;
        try
        {
            invites = await guild.GetInvitesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unable to fetch invites for guild {GuildId}", guild.Id);
            return null;
        }

        var current = invites.ToDictionary(
            invite => invite.Code,
            invite => new InviteSnapshot(invite.Uses ?? 0, invite.Inviter?.Id));

        if (!_cache.TryGetValue(guild.Id, out var previous))
        {
            _cache[guild.Id] = current;
            return null;
        }

        var match = current
            .Select(entry =>
            {
                previous.TryGetValue(entry.Key, out var oldSnapshot);
                var delta = entry.Value.Uses - oldSnapshot.Uses;
                return new { entry.Value.InviterId, Delta = delta };
            })
            .Where(x => x.Delta > 0 && x.InviterId.HasValue)
            .OrderByDescending(x => x.Delta)
            .FirstOrDefault();

        _cache[guild.Id] = current;

        return match?.InviterId;
    }

    private readonly record struct InviteSnapshot(int Uses, ulong? InviterId);
}
