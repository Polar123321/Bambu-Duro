using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Discord.Net;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Commands.Moderation;

public sealed class GiveRoleCommand : CommandBase
{
    public GiveRoleCommand(
        EmbedHelper embedHelper,
        IOptions<BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
    }

    [Command("giverole")]
    [Alias("addrole", "roleadd", "darCargo", "darcargo")]
    [Summary("Da um cargo para um usuario (somente administradores). Ex: *giverole @user @cargo")]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task GiveRoleAsync(SocketGuildUser user, SocketRole role, [Remainder] string? reason = null)
    {
        await TrackUserAsync();

        if (Context.Guild is not SocketGuild guild)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        if (user == null || role == null)
        {
            await ReplyAsync($"Uso: `{Config.Value.Prefix}giverole @user @cargo [motivo]`");
            return;
        }

        if (role.Id == guild.EveryoneRole.Id)
        {
            await ReplyAsync("Voce nao pode atribuir o cargo @everyone.");
            return;
        }

        var bot = guild.CurrentUser;
        if (!bot.GuildPermissions.ManageRoles)
        {
            await ReplyAsync("Eu nao tenho permissao de **Gerenciar Cargos**.");
            return;
        }

        if (role.Position >= bot.Hierarchy)
        {
            await ReplyAsync("Eu nao consigo atribuir esse cargo (hierarquia). Coloque meu cargo acima dele.");
            return;
        }

        if (user.Roles.Any(r => r.Id == role.Id))
        {
            await ReplyMajesticAsync("Cargo", $"{user.Mention} ja possui o cargo {role.Mention}.");
            return;
        }

        var auditReason = string.IsNullOrWhiteSpace(reason)
            ? $"giverole by {Context.User} ({Context.User.Id})"
            : $"giverole by {Context.User} ({Context.User.Id}): {reason.Trim()}";

        try
        {
            await user.AddRoleAsync(role, new RequestOptions { AuditLogReason = auditReason });

            var embed = EmbedHelper.CreateSuccess("✅ Cargo atribuido",
                    $"Usuario: {user.Mention}\nCargo: {role.Mention}")
                .WithCurrentTimestamp();

            await ReplyMajesticAsync(embed);
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.InsufficientPermissions)
        {
            await ReplyAsync("Falha ao atribuir cargo: permissoes insuficientes (ou hierarquia).");
        }
        catch (Exception ex)
        {
            await ReplyAsync($"Falha ao atribuir cargo: {ex.Message}");
        }
    }
}
