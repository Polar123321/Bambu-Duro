using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class StaffQuestionDelCommand : CommandBase
{
    private readonly IStaffApplicationStore _store;

    public StaffQuestionDelCommand(
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

    [Command("formperguntadel")]
    [Summary("Remove uma pergunta global do formulario de staff pelo indice.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task RemoveGlobalAsync(int index)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        var questions = await _store.GetGlobalQuestionsAsync(Context.Guild.Id);
        if (index <= 0 || index > questions.Count)
        {
            await ReplyAsync("Indice invalido.");
            return;
        }

        await _store.RemoveGlobalQuestionAsync(Context.Guild.Id, index - 1);
        var embed = EmbedHelper.CreateSuccess("Pergunta removida", $"Pergunta global #{index} removida.");
        await ReplyMajesticAsync(embed);
    }

    [Command("formperguntadelcargo")]
    [Summary("Remove uma pergunta de um cargo no formulario de staff pelo indice.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task RemoveRoleAsync(IRole role, int index)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        var questions = await _store.GetRoleQuestionsAsync(Context.Guild.Id, role.Id);
        if (index <= 0 || index > questions.Count)
        {
            await ReplyAsync("Indice invalido.");
            return;
        }

        await _store.RemoveRoleQuestionAsync(Context.Guild.Id, role.Id, index - 1);
        var embed = EmbedHelper.CreateSuccess("Pergunta removida", $"Pergunta #{index} removida de {role.Mention}.");
        await ReplyMajesticAsync(embed);
    }
}
