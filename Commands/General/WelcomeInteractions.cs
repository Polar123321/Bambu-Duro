using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Commands.General;

public sealed class WelcomeInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IGuildConfigStore _configStore;

    public WelcomeInteractions(IGuildConfigStore configStore)
    {
        _configStore = configStore;
    }

    [ComponentInteraction("welcome:hi:*")]
    public async Task WelcomeButtonAsync(string userIdRaw)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Este botao so funciona em servidores.", ephemeral: true);
            return;
        }

        if (!ulong.TryParse(userIdRaw, out var userId))
        {
            await RespondAsync("Usuario invalido.", ephemeral: true);
            return;
        }

        var config = await _configStore.GetAsync(Context.Guild.Id);
        if (config.ReceptionistRoleId != 0)
        {
            var member = Context.User as SocketGuildUser;
            if (member == null || !member.Roles.Any(r => r.Id == config.ReceptionistRoleId))
            {
                await RespondAsync("Somente o recepcionista pode usar este botao.", ephemeral: true);
                return;
            }
        }

        var user = Context.Guild.GetUser(userId);
        if (user == null)
        {
            await RespondAsync("Nao encontrei o usuario.", ephemeral: true);
            return;
        }

        var roleMention = config.ReceptionistRoleId != 0
            ? Context.Guild.GetRole(config.ReceptionistRoleId)?.Mention
            : null;

        var message = string.IsNullOrWhiteSpace(roleMention)
            ? $"{Context.User.Mention} deu boas-vindas para {user.Mention}!"
            : $"{Context.User.Mention} (recepcionista) deu boas-vindas para {user.Mention}!";

        var allowedMentions = AllowedMentions.None;
        allowedMentions.UserIds = new List<ulong> { user.Id };
        if (config.ReceptionistRoleId != 0)
        {
            var role = Context.Guild.GetRole(config.ReceptionistRoleId);
            if (role != null)
            {
                allowedMentions.RoleIds = new List<ulong> { role.Id };
            }
        }

        await Context.Channel.SendMessageAsync(message, allowedMentions: allowedMentions);
        await RespondAsync("Boas-vindas registradas.", ephemeral: true);
    }
}
