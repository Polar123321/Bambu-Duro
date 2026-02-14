using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;
using ConsoleApp4.Handlers;

namespace ConsoleApp4;

public sealed class BotClient
{
    private readonly DiscordSocketClient _client;
    private readonly CommandHandler _commandHandler;
    private readonly InteractionHandler _interactionHandler;
    private readonly WelcomeHandler _welcomeHandler;
    private readonly InviteTrackingHandler _inviteTrackingHandler;
    private readonly ILogger<BotClient> _logger;
    private readonly IOptions<BotConfiguration> _config;

    public BotClient(
        DiscordSocketClient client,
        CommandHandler commandHandler,
        InteractionHandler interactionHandler,
        WelcomeHandler welcomeHandler,
        InviteTrackingHandler inviteTrackingHandler,
        ILogger<BotClient> logger,
        IOptions<BotConfiguration> config)
    {
        _client = client;
        _commandHandler = commandHandler;
        _interactionHandler = interactionHandler;
        _welcomeHandler = welcomeHandler;
        _inviteTrackingHandler = inviteTrackingHandler;
        _logger = logger;
        _config = config;
    }

    public async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.Value.Token))
        {
            throw new InvalidOperationException("Bot token is missing. Configure Bot:Token in appsettings.json or environment variables.");
        }

        
        _client.Log -= OnLogAsync;
        _client.Ready -= OnReadyAsync;
        _client.Log += OnLogAsync;
        _client.Ready += OnReadyAsync;

        await _commandHandler.InitializeAsync();
        await _interactionHandler.InitializeAsync();
        await _welcomeHandler.InitializeAsync();
        await _inviteTrackingHandler.InitializeAsync();

        await _client.LoginAsync(TokenType.Bot, _config.Value.Token);
        await _client.StartAsync();

        _logger.LogInformation("Bot started and awaiting events.");
    }

    public async Task StopAsync()
    {
        try
        {
            await _client.StopAsync();
            await _client.LogoutAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to stop bot cleanly.");
        }
        finally
        {
            _client.Log -= OnLogAsync;
            _client.Ready -= OnReadyAsync;
        }
    }

    private async Task OnReadyAsync()
    {
        _logger.LogInformation("{User} is online.", _client.CurrentUser);

        if (!string.IsNullOrWhiteSpace(_config.Value.Status))
        {
            await _client.SetGameAsync(_config.Value.Status);
        }
    }

    private Task OnLogAsync(LogMessage message)
    {
        
        if (message.Message != null &&
            message.Message.StartsWith("Command: null", StringComparison.OrdinalIgnoreCase) &&
            message.Exception == null)
        {
            return Task.CompletedTask;
        }

        var level = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        _logger.Log(level, message.Exception, "{Source}: {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }
}
