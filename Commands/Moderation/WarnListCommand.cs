using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class WarnListCommand : CommandBase
{
    private const int PageSize = 25; 
    private readonly IWarnService _warns;

    public WarnListCommand(
        IWarnService warns,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _warns = warns;
    }

    [Command("warnlist")]
    [Alias("warnings", "avisos")]
    [Summary("Lista usuarios warnados e permite ver/revogar avisos via dropdown.")]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task WarnListAsync(int page = 1)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        var all = await _warns.GetWarnedUsersAsync(Context.Guild.Id);
        if (all.Count == 0)
        {
            await ReplyMajesticAsync("Warns", "Nenhum usuario possui warns ativos neste servidor.");
            return;
        }

        var totalPages = (int)Math.Ceiling(all.Count / (double)PageSize);
        var safePage = Math.Clamp(page, 1, Math.Max(totalPages, 1));
        var slice = all.Skip((safePage - 1) * PageSize).Take(PageSize).ToList();

        var embed = EmbedHelper.CreateWarning("⚠️ Warnlist",
                $"Selecione um usuario para ver os warns.\nPagina {safePage}/{Math.Max(totalPages, 1)}")
            .WithCurrentTimestamp();

        var select = new SelectMenuBuilder()
            .WithCustomId($"warn:list:select:{safePage}")
            .WithPlaceholder("Selecione um usuario warnado...")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var u in slice)
        {
            var label = TryGetDisplayName((SocketGuild)Context.Guild, u.UserId);
            if (label.Length > 90)
            {
                label = label[..90];
            }

            select.AddOption(
                label: label,
                value: u.UserId.ToString(),
                description: $"{u.ActiveCount} warn(s) ativo(s)");
        }

        var components = EmbedHelper.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[] { select });

            if (totalPages > 1)
            {
                c.WithActionRow(new[]
                {
                    new ButtonBuilder()
                        .WithLabel("◀")
                        .WithCustomId($"warn:list:prev:{safePage}")
                        .WithStyle(ButtonStyle.Secondary)
                        .WithDisabled(safePage <= 1),
                    new ButtonBuilder()
                        .WithLabel("▶")
                        .WithCustomId($"warn:list:next:{safePage}")
                        .WithStyle(ButtonStyle.Secondary)
                        .WithDisabled(safePage >= totalPages)
                });
            }
        });

        await ReplyAsync(components: components);
    }

    private static string TryGetDisplayName(SocketGuild guild, ulong userId)
    {
        if (guild.GetUser(userId) is { } user)
        {
            return string.IsNullOrWhiteSpace(user.Nickname) ? user.Username : user.Nickname;
        }

        
        return $"User {userId}";
    }
}
