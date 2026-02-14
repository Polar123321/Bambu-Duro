using Discord.Commands;
using Microsoft.Extensions.Hosting;
using ConsoleApp4.Attributes;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class ShutdownCommand : CommandBase
{
    private readonly IHostApplicationLifetime _lifetime;

    public ShutdownCommand(
        IHostApplicationLifetime lifetime,
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _lifetime = lifetime;
    }

    [Command("shutdown")]
    [Summary("Encerra o bot com segurança (somente owner).")]
    [RequireBotOwner]
    public async Task ShutdownAsync()
    {
        await ReplyAsync("Encerrando o bot com segurança...");
        _lifetime.StopApplication();
    }
}
