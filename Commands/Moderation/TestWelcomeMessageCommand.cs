using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Handlers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class TestWelcomeMessageCommand : CommandBase
{
    private readonly IGuildConfigStore _configStore;
    private readonly WelcomeHandler _welcomeHandler;
    private readonly ILogger<TestWelcomeMessageCommand> _logger;

    public TestWelcomeMessageCommand(
        IGuildConfigStore configStore,
        WelcomeHandler welcomeHandler,
        EmbedHelper embedHelper,
        ILogger<TestWelcomeMessageCommand> logger,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
        _configStore = configStore;
        _welcomeHandler = welcomeHandler;
        _logger = logger;
    }

    [Command("testwelcomemessage")]
    [Alias("testwelcome", "testbemvindo", "testbemvinda")]
    [Summary("Envia uma pre-visualizacao da mensagem de boas-vindas.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task TestWelcomeMessageAsync()
    {
        _logger.LogInformation("TestWelcomeMessage invoked by {UserId} in {GuildId}", Context.User.Id, Context.Guild?.Id);
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            _logger.LogWarning("TestWelcomeMessage aborted: no guild context for {UserId}", Context.User.Id);
            await ReplyAsync("Este comando so pode ser usado em servidores.");
            return;
        }

        var config = await _configStore.GetAsync(Context.Guild.Id);
        _logger.LogInformation("Welcome config loaded: WelcomeChannelId={ChannelId} ReceptionistRoleId={RoleId}", config.WelcomeChannelId, config.ReceptionistRoleId);
        var socketChannel = config.WelcomeChannelId != 0
            ? Context.Guild.GetTextChannel(config.WelcomeChannelId)
            : Context.Channel as SocketTextChannel;

        if (socketChannel == null)
        {
            _logger.LogWarning("TestWelcomeMessage aborted: channel not found. ChannelId={ChannelId}", config.WelcomeChannelId);
            await ReplyAsync("Nao consegui identificar o canal.");
            return;
        }

        var member = Context.User as SocketGuildUser;
        if (member == null)
        {
            _logger.LogWarning("TestWelcomeMessage aborted: user not a SocketGuildUser. UserId={UserId}", Context.User.Id);
            await ReplyAsync("Nao consegui identificar o usuario.");
            return;
        }

        var botUser = Context.Guild.CurrentUser;
        var perms = botUser.GetPermissions(socketChannel);
        if (!perms.SendMessages || !perms.EmbedLinks)
        {
            var missing = new List<string>();
            if (!perms.SendMessages) missing.Add("Enviar mensagens");
            if (!perms.EmbedLinks) missing.Add("Incorporar links");
            await ReplyAsync($"Sem permissao no canal {socketChannel.Mention}: {string.Join(", ", missing)}.");
            return;
        }

        _logger.LogInformation("Sending welcome test to channel {ChannelId} for user {UserId}", socketChannel.Id, member.Id);
        try
        {
            await _welcomeHandler.SendWelcomeAsync(Context.Guild, socketChannel, member);
            _logger.LogInformation("Welcome test sent to channel {ChannelId}", socketChannel.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TestWelcomeMessage failed to send");
            await ReplyAsync($"Falha ao enviar boas-vindas: {ex.Message}");
            return;
        }

        if (socketChannel.Id != Context.Channel.Id)
        {
            await ReplyAsync($"Mensagem de teste enviada em {socketChannel.Mention}.");
        }
    }
}
