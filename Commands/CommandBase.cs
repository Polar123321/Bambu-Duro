using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands;

public abstract class CommandBase : ModuleBase<SocketCommandContext>
{
    protected CommandBase(
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
    {
        EmbedHelper = embedHelper;
        Config = config;
        UserService = userService;
        GuildService = guildService;
        CommandLogService = commandLogService;
    }

    protected EmbedHelper EmbedHelper { get; }

    protected IOptions<BotConfiguration> Config { get; }

    protected IUserService UserService { get; }

    protected IGuildService GuildService { get; }

    protected ICommandLogService CommandLogService { get; }

    protected async Task TrackUserAsync()
    {
        await UserService.GetOrCreateAsync(Context.User.Id, Context.User.Username);

        if (Context.Guild != null)
        {
            await GuildService.GetOrCreateAsync(Context.Guild.Id, Context.Guild.Name);
        }
    }

    protected Task ReplyMajesticAsync(EmbedBuilder embed)
    {
        var components = EmbedHelper.BuildCv2(embed);
        return ReplyAsync(components: components);
    }

    protected Task ReplyMajesticAsync(string title, string description)
    {
        var embed = EmbedHelper.CreateMajestic(title, description);
        return ReplyMajesticAsync(embed);
    }
}
