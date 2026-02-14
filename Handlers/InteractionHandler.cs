using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace ConsoleApp4.Handlers;

public sealed class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly ILogger<InteractionHandler> _logger;

    public InteractionHandler(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        ILogger<InteractionHandler> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _client.Ready += RegisterCommandsAsync;
        _client.InteractionCreated += HandleInteractionAsync;

        _interactions.Log += OnLogAsync;
        await _interactions.AddModulesAsync(typeof(InteractionHandler).Assembly, _services);
    }

    private async Task RegisterCommandsAsync()
    {
        try
        {
            await _interactions.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Slash commands registered.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register slash commands.");
        }
    }

    private async Task HandleInteractionAsync(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(_client, interaction);
        var result = await _interactions.ExecuteCommandAsync(context, _services);

        if (!result.IsSuccess)
        {
            _logger.LogWarning("Interaction error: {Error} - {Reason}", result.Error, result.ErrorReason);
        }
    }

    private Task OnLogAsync(LogMessage message)
    {
        _logger.LogInformation("{Source}: {Message}", message.Source, message.Message);
        return Task.CompletedTask;
    }
}
