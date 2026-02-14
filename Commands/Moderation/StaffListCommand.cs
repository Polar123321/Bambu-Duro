using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class StaffListCommand : CommandBase
{
    private readonly IStaffApplicationStore _store;

    public StaffListCommand(
        IStaffApplicationStore store,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _store = store;
    }

    [Command("stafflist")]
    [Summary("Lista todas as candidaturas a staff.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task StaffListAsync(int page = 1)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        const int pageSize = 5;
        var safePage = Math.Max(1, page);
        var all = await _store.GetAllAsync(Context.Guild.Id);
        if (all.Count == 0)
        {
            await ReplyAsync("Nenhuma candidatura encontrada.");
            return;
        }

        var totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
        safePage = Math.Clamp(safePage, 1, Math.Max(totalPages, 1));

        var embed = EmbedHelper.CreateInfo("Candidaturas a Staff",
                $"Pagina {safePage}/{Math.Max(totalPages, 1)}")
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());

        foreach (var entry in all.Skip((safePage - 1) * pageSize).Take(pageSize))
        {
            var shortMotivation = entry.Motivation.Length > 80
                ? entry.Motivation[..80] + "..."
                : entry.Motivation;

            embed.AddField($"{entry.Username} ({entry.UserId})",
                $"Status: {entry.Status}\nEnviado em: {entry.SubmittedAtUtc:dd/MM/yyyy HH:mm}\nMotivacao: {shortMotivation}");
        }

        var components = EmbedHelper.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("◀")
                    .WithCustomId($"staff:list:prev:{safePage}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage <= 1),
                new ButtonBuilder()
                    .WithLabel("▶")
                    .WithCustomId($"staff:list:next:{safePage}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage >= totalPages)
            });
        });

        await ReplyAsync(components: components);
    }
}
