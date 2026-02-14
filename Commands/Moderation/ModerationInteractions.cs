using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Commands.Moderation;

public sealed class ModerationInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IModerationActionStore _store;
    private readonly EmbedHelper _embeds;
    private readonly IWarnService _warns;

    public ModerationInteractions(IModerationActionStore store, EmbedHelper embeds, IWarnService warns)
    {
        _store = store;
        _embeds = embeds;
        _warns = warns;
    }

    [ComponentInteraction("mod:confirm:*")]
    public async Task ConfirmAsync(string token)
    {
        if (!_store.TryGet(token, out var action))
        {
            await RespondAsync("Esta confirmação expirou.", ephemeral: true);
            return;
        }

        if (action.RequestedById != Context.User.Id)
        {
            await RespondAsync("Somente quem solicitou pode confirmar.", ephemeral: true);
            return;
        }

        _store.Remove(token);

        var guild = Context.Guild;
        if (guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        
        await DeferAsync(ephemeral: true);

        
        ITextChannel? channel = null;
        if (action.Type is ModerationActionType.Clear or ModerationActionType.TestMessage)
        {
            channel = guild.GetTextChannel(action.ChannelId);
            if (channel == null)
            {
                await FollowupAsync("Canal indisponivel.", ephemeral: true);
                return;
            }
        }

        try
        {
            switch (action.Type)
            {
                case ModerationActionType.Clear:
                    await HandleClearAsync(channel!, action);
                    break;
                case ModerationActionType.Ban:
                    await HandleBanAsync(guild, action);
                    break;
                case ModerationActionType.Kick:
                    await HandleKickAsync(guild, action);
                    break;
                case ModerationActionType.Mute:
                    await HandleMuteAsync(guild, action);
                    break;
                case ModerationActionType.Warn:
                    await HandleWarnAsync(guild, action);
                    break;
                case ModerationActionType.TestMessage:
                    await HandleTestMessageAsync(channel!, action);
                    break;
            }
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Falha ao executar a acao: {ex.Message}", ephemeral: true);
        }
    }

    [ComponentInteraction("mod:cancel:*")]
    public async Task CancelAsync(string token)
    {
        if (_store.TryGet(token, out var action) && action.RequestedById != Context.User.Id)
        {
            await RespondAsync("Somente quem solicitou pode cancelar.", ephemeral: true);
            return;
        }

        _store.Remove(token);
        await RespondAsync("Ação cancelada.", ephemeral: true);
    }

    private async Task HandleClearAsync(ITextChannel channel, ModerationAction action)
    {
        var messages = await channel.GetMessagesAsync(action.Amount + 1).FlattenAsync();
        await channel.DeleteMessagesAsync(messages);

        var embed = _embeds.CreateSuccess("🧹 Limpeza concluida",
                $"Mensagens apagadas: **{action.Amount}**\nMotivo: **{action.Reason}**");

        await FollowupAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    private async Task HandleBanAsync(SocketGuild guild, ModerationAction action)
    {
        var moderator = await ResolveGuildUserAsync(guild, action.RequestedById);
        if (moderator == null)
        {
            await FollowupAsync("Moderador que iniciou a acao nao foi encontrado.", ephemeral: true);
            return;
        }

        var user = await ResolveGuildUserAsync(guild, action.TargetUserId);
        if (user == null)
        {
            await FollowupAsync("Usuario nao encontrado.", ephemeral: true);
            return;
        }

        if (!CanModerateTarget(guild, moderator, user))
        {
            await FollowupAsync("Voce nao pode banir esse usuario (hierarquia).", ephemeral: true);
            return;
        }

        await guild.AddBanAsync(action.TargetUserId, 0, action.Reason);

        var embed = _embeds.CreateSuccess("⛔ Ban aplicado",
                $"Usuario: **{action.TargetUserId}**\nMotivo: **{action.Reason}**");

        await FollowupAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    private async Task HandleKickAsync(SocketGuild guild, ModerationAction action)
    {
        var moderator = await ResolveGuildUserAsync(guild, action.RequestedById);
        if (moderator == null)
        {
            await FollowupAsync("Moderador que iniciou a acao nao foi encontrado.", ephemeral: true);
            return;
        }

        var user = await ResolveGuildUserAsync(guild, action.TargetUserId);
        if (user == null)
        {
            await FollowupAsync("Usuario nao encontrado.", ephemeral: true);
            return;
        }

        if (!CanModerateTarget(guild, moderator, user))
        {
            await FollowupAsync("Voce nao pode expulsar esse usuario (hierarquia).", ephemeral: true);
            return;
        }

        await user.KickAsync(action.Reason);
        var embed = _embeds.CreateSuccess("🥾 Kick aplicado",
                $"Usuario: **{user.Username}**\nMotivo: **{action.Reason}**");

        await FollowupAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    private async Task HandleMuteAsync(SocketGuild guild, ModerationAction action)
    {
        var moderator = await ResolveGuildUserAsync(guild, action.RequestedById);
        if (moderator == null)
        {
            await FollowupAsync("Moderador que iniciou a acao nao foi encontrado.", ephemeral: true);
            return;
        }

        var user = await ResolveGuildUserAsync(guild, action.TargetUserId);
        if (user == null)
        {
            await FollowupAsync("Usuario nao encontrado.", ephemeral: true);
            return;
        }

        if (!CanModerateTarget(guild, moderator, user))
        {
            await FollowupAsync("Voce nao pode aplicar mute nesse usuario (hierarquia).", ephemeral: true);
            return;
        }

        var duration = TimeSpan.FromMinutes(action.DurationMinutes);
        await user.SetTimeOutAsync(duration, new RequestOptions { AuditLogReason = action.Reason });

        var embed = _embeds.CreateSuccess("🔇 Mute aplicado",
                $"Usuario: **{user.Username}**\nTempo: **{action.DurationMinutes} min**\nMotivo: **{action.Reason}**");

        await FollowupAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    private async Task HandleWarnAsync(SocketGuild guild, ModerationAction action)
    {
        var moderator = await ResolveGuildUserAsync(guild, action.RequestedById);
        if (moderator == null)
        {
            await FollowupAsync("Moderador que iniciou a acao nao foi encontrado.", ephemeral: true);
            return;
        }

        var user = await ResolveGuildUserAsync(guild, action.TargetUserId);
        if (user == null)
        {
            await FollowupAsync("Usuario nao encontrado.", ephemeral: true);
            return;
        }

        if (!CanModerateTarget(guild, moderator, user))
        {
            await FollowupAsync("Voce nao pode aplicar warn nesse usuario (hierarquia).", ephemeral: true);
            return;
        }

        await _warns.AddWarnAsync(action.GuildId, action.TargetUserId, action.RequestedById, action.Reason, action.CreatedAtUtc);
        await AllWarnListLivePanel.TryRefreshAsync(Context.Client, _embeds, _warns, guild.Id);

        var embed = _embeds.CreateWarning("⚠️ Aviso aplicado",
                $"Usuario: **{user.Username}**\nMotivo: **{action.Reason}**");

        await FollowupAsync(components: _embeds.BuildCv2(embed), ephemeral: true);

        try
        {
            await user.SendMessageAsync(components: _embeds.BuildCv2(embed));
        }
        catch
        {
            
        }
    }

    private async Task<IGuildUser?> ResolveGuildUserAsync(SocketGuild guild, ulong userId)
    {
        
        var cached = guild.GetUser(userId);
        if (cached != null)
        {
            return cached;
        }

        try
        {
            return await Context.Client.Rest.GetGuildUserAsync(guild.Id, userId);
        }
        catch
        {
            return null;
        }
    }

    private async Task HandleTestMessageAsync(ITextChannel channel, ModerationAction action)
    {
        try
        {
            await channel.SendMessageAsync(action.Reason);

            var embed = _embeds.CreateSuccess("✅ Mensagem de teste enviada",
                    $"Canal: <#{action.ChannelId}>");

            await FollowupAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Falha ao enviar mensagem de teste: {ex.Message}", ephemeral: true);
        }
    }

    private static bool CanModerateTarget(SocketGuild guild, IGuildUser moderator, IGuildUser target)
    {
        if (moderator.Id == target.Id)
        {
            return false;
        }

        if (target.Id == guild.OwnerId)
        {
            return false;
        }

        if (moderator.Id == guild.OwnerId)
        {
            return true;
        }

        return moderator.Hierarchy > target.Hierarchy;
    }
}
