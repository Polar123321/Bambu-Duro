using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.User;

public sealed class AvatarCommand : CommandBase
{
    public AvatarCommand(
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
    }

    [Command("avatar")]
    [Summary("Mostra seu avatar em alta qualidade.")]
    public async Task AvatarAsync()
    {
        await TrackUserAsync();

        var avatarUrl = Context.User.GetAvatarUrl(size: 1024) ?? Context.User.GetDefaultAvatarUrl();
        var embed = EmbedHelper.CreateInfo($"🎨 Avatar de {Context.User.Username}", "Clique para abrir em tamanho original")
            .WithImageUrl(avatarUrl);

        await ReplyMajesticAsync(embed);
    }
}
