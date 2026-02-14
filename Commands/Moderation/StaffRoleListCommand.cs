using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class StaffRoleListCommand : CommandBase
{
    private readonly IStaffApplicationStore _store;

    public StaffRoleListCommand(
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

    [Command("formcargolist")]
    [Summary("Lista os cargos disponiveis no formulario de staff.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task ListAsync()
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        var roleIds = await _store.GetRolesAsync(Context.Guild.Id);
        if (roleIds.Count == 0)
        {
            await ReplyAsync("Nenhum cargo configurado.");
            return;
        }

        var lines = roleIds
            .Select(id => Context.Guild.GetRole(id))
            .Where(r => r != null)
            .Select(r => r!.Mention)
            .ToArray();

        var embed = EmbedHelper.CreateInfo("Cargos do formulario", string.Join(" ", lines));
        await ReplyMajesticAsync(embed);
    }
}
