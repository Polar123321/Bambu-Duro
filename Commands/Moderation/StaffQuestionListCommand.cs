using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class StaffQuestionListCommand : CommandBase
{
    private readonly IStaffApplicationStore _store;

    public StaffQuestionListCommand(
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

    [Command("formperguntalist")]
    [Summary("Lista perguntas globais do formulario de staff.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task ListGlobalAsync()
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        var questions = await _store.GetGlobalQuestionsAsync(Context.Guild.Id);
        if (questions.Count == 0)
        {
            await ReplyAsync("Nenhuma pergunta global configurada.");
            return;
        }

        var lines = questions.Select((q, i) => $"{i + 1}. {q}");
        var embed = EmbedHelper.CreateInfo("Perguntas globais", string.Join("\n", lines));
        await ReplyMajesticAsync(embed);
    }

    [Command("formperguntalistcargo")]
    [Summary("Lista perguntas do formulario de staff para um cargo.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task ListRoleAsync(IRole role)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        var questions = await _store.GetRoleQuestionsAsync(Context.Guild.Id, role.Id);
        if (questions.Count == 0)
        {
            await ReplyAsync($"Nenhuma pergunta configurada para {role.Mention}.");
            return;
        }

        var lines = questions.Select((q, i) => $"{i + 1}. {q}");
        var embed = EmbedHelper.CreateInfo($"Perguntas - {role.Name}", string.Join("\n", lines));
        await ReplyMajesticAsync(embed);
    }
}
