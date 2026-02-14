using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class StaffRoleDelCommand : CommandBase
{
    private readonly IStaffApplicationStore _store;

    public StaffRoleDelCommand(
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

    [Command("formcargodel")]
    [Summary("Remove um cargo do formulario de staff.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task RemoveAsync(IRole role)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        var roleIds = await _store.GetRolesAsync(Context.Guild.Id);
        if (!roleIds.Contains(role.Id))
        {
            await ReplyAsync("Esse cargo nao esta configurado.");
            return;
        }

        var updated = roleIds.Where(id => id != role.Id).ToList();
        await _store.SetRolesAsync(Context.Guild.Id, updated);

        var embed = EmbedHelper.CreateSuccess("Cargo removido", $"Cargo {role.Mention} removido do formulario.");
        await ReplyMajesticAsync(embed);
    }
}
