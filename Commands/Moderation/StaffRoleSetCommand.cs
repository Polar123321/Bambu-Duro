using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class StaffRoleSetCommand : CommandBase
{
    private readonly IStaffApplicationStore _store;

    public StaffRoleSetCommand(
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

    [Command("formcargoset")]
    [Summary("Adiciona um cargo disponivel no formulario de staff.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task AddRoleAsync(IRole role)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        await _store.AddRoleAsync(Context.Guild.Id, role.Id);
        var embed = EmbedHelper.CreateSuccess("Cargo adicionado", $"Cargo {role.Mention} disponivel no formulario.");
        await ReplyMajesticAsync(embed);
    }
}
