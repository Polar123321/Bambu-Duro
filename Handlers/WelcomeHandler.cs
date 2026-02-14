using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Handlers;

public sealed class WelcomeHandler
{
    private readonly DiscordSocketClient _client;
    private readonly IGuildConfigStore _configStore;
    private readonly WaifuPicsClient _waifu;
    private readonly EmbedHelper _embeds;
    private readonly ILogger<WelcomeHandler> _logger;

    public WelcomeHandler(
        DiscordSocketClient client,
        IGuildConfigStore configStore,
        WaifuPicsClient waifu,
        EmbedHelper embeds,
        ILogger<WelcomeHandler> logger)
    {
        _client = client;
        _configStore = configStore;
        _waifu = waifu;
        _embeds = embeds;
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        _client.UserJoined += OnUserJoinedAsync;
        return Task.CompletedTask;
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        try
        {
            var config = await _configStore.GetAsync(user.Guild.Id);
            if (config.WelcomeChannelId == 0)
            {
                return;
            }

            var channel = user.Guild.GetTextChannel(config.WelcomeChannelId);
            if (channel == null)
            {
                return;
            }

            await SendWelcomeAsync(user.Guild, channel, user);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send welcome message for {GuildId}", user.Guild.Id);
        }
    }

    public async Task SendWelcomeAsync(SocketGuild guild, ISocketMessageChannel channel, SocketGuildUser user)
    {
        var config = await _configStore.GetAsync(guild.Id);

        var description = ExpandTemplate(config.WelcomeDescription, guild, user);
        var title = string.IsNullOrWhiteSpace(config.WelcomeTitle)
            ? "Bem-vindo(a)!"
            : config.WelcomeTitle.Trim();

        var imageUrl = string.IsNullOrWhiteSpace(config.WelcomeImageUrl)
            ? null
            : config.WelcomeImageUrl.Trim();

        if (string.IsNullOrWhiteSpace(imageUrl) && config.WelcomeUseWaifuImage)
        {
            imageUrl = await _waifu.GetImageUrlAsync("happy", nsfw: false)
                       ?? await _waifu.GetImageUrlAsync("smile", nsfw: false)
                       ?? await _waifu.GetImageUrlAsync("waifu", nsfw: false);
        }

        var allowedMentions = AllowedMentions.None;
        allowedMentions.UserIds = new List<ulong> { user.Id };
        string? receptionistMention = null;

        if (config.ReceptionistRoleId != 0)
        {
            var role = guild.GetRole(config.ReceptionistRoleId);
            if (role != null)
            {
                allowedMentions.RoleIds = new List<ulong> { role.Id };
                receptionistMention = role.Mention;
            }
        }

        var embed = BuildWelcomeEmbed(title, description, imageUrl, config);

        if (channel is SocketTextChannel textChannel)
        {
            var botUser = guild.CurrentUser;
            var perms = botUser.GetPermissions(textChannel);
            if (!perms.SendMessages || !perms.EmbedLinks)
            {
                _logger.LogWarning("Missing permissions in channel {ChannelId}: SendMessages={Send} EmbedLinks={Embed}",
                    textChannel.Id, perms.SendMessages, perms.EmbedLinks);
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(receptionistMention))
        {
            await channel.SendMessageAsync(
                $"{receptionistMention} recepcione {user.Mention}!",
                allowedMentions: allowedMentions);
        }

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel(GetButtonLabel(config))
                    .WithCustomId($"welcome:hi:{user.Id}")
                    .WithStyle(ButtonStyle.Primary)
            });
        });

        await channel.SendMessageAsync(components: components, allowedMentions: allowedMentions);
    }

    private static string ExpandTemplate(string template, SocketGuild guild, SocketGuildUser user)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return $"Bem-vindo(a) {user.Mention} ao **{guild.Name}**!\n" +
                   $"Agora somos **{guild.MemberCount}** membros.";
        }

        return template
            .Replace("{user}", user.Mention, StringComparison.OrdinalIgnoreCase)
            .Replace("{userMention}", user.Mention, StringComparison.OrdinalIgnoreCase)
            .Replace("{userName}", user.Username, StringComparison.OrdinalIgnoreCase)
            .Replace("{guild}", guild.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{memberCount}", guild.MemberCount.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetButtonLabel(GuildConfig config)
    {
        return string.IsNullOrWhiteSpace(config.WelcomeButtonLabel)
            ? "Dar boas-vindas"
            : config.WelcomeButtonLabel.Trim();
    }

    private EmbedBuilder BuildWelcomeEmbed(string title, string description, string? imageUrl, GuildConfig config)
    {
        
        
        var embed = _embeds.CreateMajestic(title, description, null);

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            embed.WithImageUrl(imageUrl);
        }

        embed.WithColor(ParseColor(config.WelcomeColor));

        return embed;
    }

    private static Discord.Color ParseColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Discord.Color.Gold;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "gold" => Discord.Color.Gold,
            "blue" => Discord.Color.Blue,
            "teal" => Discord.Color.Teal,
            "green" => Discord.Color.Green,
            "red" => Discord.Color.Red,
            "purple" => Discord.Color.Purple,
            "magenta" => Discord.Color.Magenta,
            "orange" => Discord.Color.Orange,
            _ => Discord.Color.Gold
        };
    }
}
