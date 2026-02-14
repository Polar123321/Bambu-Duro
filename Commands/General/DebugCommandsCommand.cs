using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.General;

public sealed class DebugCommandsCommand : CommandBase
{
    private readonly CommandService _commands;

    public DebugCommandsCommand(
        CommandService commands,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _commands = commands;
    }

    [Command("debugcommands")]
    [Summary("Lista os comandos carregados.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task DebugCommandsAsync()
    {
        var names = _commands.Commands
            .SelectMany(c => c.Aliases)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var text = names.Count == 0
            ? "Nenhum comando carregado."
            : string.Join(", ", names);

        var embed = EmbedHelper.CreateInfo("Comandos carregados", text);
        await ReplyMajesticAsync(embed);
    }
}
