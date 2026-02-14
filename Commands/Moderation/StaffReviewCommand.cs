using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class StaffReviewCommand : CommandBase
{
    public StaffReviewCommand(
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
    }

    [Command("staffreview")]
    [Summary("Abre o formulario de consulta de candidaturas.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task StaffReviewAsync()
    {
        await TrackUserAsync();

        var embed = EmbedHelper.CreateInfo("🗂️ Revisar candidaturas",
                "Clique para abrir o formulario de busca por usuario.");

        var components = EmbedHelper.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Abrir formulario")
                    .WithCustomId("staff:admin:open")
                    .WithStyle(ButtonStyle.Primary)
            });
        });

        await ReplyAsync(components: components);
    }
}
