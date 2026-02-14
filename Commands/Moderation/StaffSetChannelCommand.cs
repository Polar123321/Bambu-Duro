using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class StaffSetChannelCommand : CommandBase
{
    private readonly IStaffApplicationStore _store;

    public StaffSetChannelCommand(
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

    [Command("staffsetchannel")]
    [Summary("Define o canal onde as candidaturas serao postadas.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task SetChannelAsync(ITextChannel? channel = null)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        if (channel == null)
        {
            var current = await _store.GetChannelAsync(Context.Guild.Id);
            if (current == 0)
            {
                await ReplyAsync("Nenhum canal configurado. Use !staffsetchannel #canal");
                return;
            }

            await ReplyAsync($"Canal atual: <#{current}>");
            return;
        }

        await _store.SetChannelAsync(Context.Guild.Id, channel.Id);
        var embed = EmbedHelper.CreateSuccess("Canal definido", $"Candidaturas serao enviadas em {channel.Mention}.");
        await ReplyMajesticAsync(embed);
    }
}
