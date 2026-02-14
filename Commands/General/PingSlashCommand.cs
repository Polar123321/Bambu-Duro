using Discord.Interactions;

namespace ConsoleApp4.Commands.General;

public sealed class PingSlashCommand : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("ping", "Verifica a latência do bot.")]
    public async Task PingAsync()
    {
        await RespondAsync($"Pong! Latencia: {Context.Client.Latency}ms", ephemeral: true);
    }
}
