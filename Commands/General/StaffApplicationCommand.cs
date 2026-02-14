using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.General;

public sealed class StaffApplicationCommand : CommandBase
{
    private readonly IStaffApplicationStore _store;

    public StaffApplicationCommand(
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

    [Command("staff")]
    [Alias("staffapply", "aplicar")]
    [Summary("Abre o formulario para candidatura a staff.")]
    public async Task StaffApplyAsync()
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so pode ser usado em servidores.");
            return;
        }

        var roleIds = await _store.GetRolesAsync(Context.Guild.Id);
        if (roleIds.Count == 0)
        {
            await ReplyAsync("Nenhum cargo configurado. Fale com um administrador.");
            return;
        }

        var embed = EmbedHelper.CreateInfo("Candidatura a Staff",
                "Escolha um cargo e depois responda o formulario.")
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());

        var select = new SelectMenuBuilder()
            .WithCustomId("staff:role:select")
            .WithPlaceholder("Escolha o cargo desejado")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var roleId in roleIds)
        {
            var role = Context.Guild.GetRole(roleId);
            if (role != null)
            {
                select.AddOption(role.Name, role.Id.ToString(), $"Cargo: {role.Name}");
            }
        }

        var components = EmbedHelper.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[] { select });
        });

        await ReplyAsync(components: components);
    }
}
