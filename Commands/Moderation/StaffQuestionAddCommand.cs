using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class StaffQuestionAddCommand : CommandBase
{
    private readonly IStaffApplicationStore _store;

    public StaffQuestionAddCommand(
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

    [Command("formperguntaadd")]
    [Summary("Adiciona uma pergunta global ao formulario de staff.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task AddGlobalAsync([Remainder] string question)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            await ReplyAsync("Use: formperguntaadd <pergunta>");
            return;
        }

        await _store.AddGlobalQuestionAsync(Context.Guild.Id, question);
        var embed = EmbedHelper.CreateSuccess("Pergunta adicionada", "Pergunta global adicionada ao formulario.");
        await ReplyMajesticAsync(embed);
    }

    [Command("formperguntaaddcargo")]
    [Summary("Adiciona uma pergunta especifica para um cargo no formulario de staff.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task AddRoleAsync(IRole role, [Remainder] string question)
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            await ReplyAsync("Use: formperguntaaddcargo @cargo <pergunta>");
            return;
        }

        await _store.AddRoleQuestionAsync(Context.Guild.Id, role.Id, question);
        var embed = EmbedHelper.CreateSuccess("Pergunta adicionada", $"Pergunta adicionada para o cargo {role.Mention}.");
        await ReplyMajesticAsync(embed);
    }
}
