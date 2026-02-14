using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class ReceptionistRoleCommand : CommandBase
{
    private readonly IGuildConfigStore _configStore;

    public ReceptionistRoleCommand(
        IGuildConfigStore configStore,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _configStore = configStore;
    }

    [Command("setrecepcionista")]
    [Alias("setreceptionist", "recepcionista")]
    [Summary("Define o cargo de recepcionista do servidor.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task SetReceptionistRoleAsync(IRole? role = null)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so pode ser usado em servidores.");
            return;
        }

        var targetRole = role;
        if (targetRole == null)
        {
            await ReplyAsync("Use: setrecepcionista @cargo");
            return;
        }

        var config = await _configStore.GetAsync(Context.Guild.Id);
        config.ReceptionistRoleId = targetRole.Id;
        await _configStore.SaveAsync(Context.Guild.Id, config);

        var embed = EmbedHelper.CreateSuccess("Cargo definido!",
            $"O cargo de recepcionista agora e {targetRole.Mention}.");

        await ReplyMajesticAsync(embed);
    }
}
