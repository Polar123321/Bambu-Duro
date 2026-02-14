using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;
using System.Text;

namespace ConsoleApp4.Commands.Moderation;

public sealed class WarnInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private const int PageSize = 25;
    private readonly IWarnService _warns;
    private readonly EmbedHelper _embeds;

    public WarnInteractions(IWarnService warns, EmbedHelper embeds)
    {
        _warns = warns;
        _embeds = embeds;
    }

    [ComponentInteraction("warn:list:prev:*")]
    public async Task PrevAsync(string currentPage)
    {
        if (!int.TryParse(currentPage, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await UpdateListAsync(Math.Max(1, page - 1));
    }

    [ComponentInteraction("warn:list:next:*")]
    public async Task NextAsync(string currentPage)
    {
        if (!int.TryParse(currentPage, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await UpdateListAsync(page + 1);
    }

    [ComponentInteraction("warn:list:select:*")]
    public async Task SelectAsync(string pageRaw, string[] selections)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync("Nao consegui atualizar a warnlist.", ephemeral: true);
            return;
        }

        if (!int.TryParse(pageRaw, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        if (selections.Length == 0 || !ulong.TryParse(selections[0], out var userId))
        {
            await RespondAsync("Selecao invalida.", ephemeral: true);
            return;
        }

        var warns = await _warns.GetActiveWarnsAsync(Context.Guild.Id, userId);
        var embed = _embeds.CreateWarning("⚠️ Warns do usuario", BuildWarnDetails(userId, warns))
            .WithCurrentTimestamp();

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            if (warns.Count > 0)
            {
                var revoke = new SelectMenuBuilder()
                    .WithCustomId($"warn:revoke:select:{userId}:{page}")
                    .WithPlaceholder("Revogar um warn...")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                foreach (var w in warns.Take(25))
                {
                    var label = w.CreatedAtUtc.ToLocalTime().ToString("dd/MM HH:mm");
                    var desc = w.Reason.Length > 80 ? w.Reason[..80] + "..." : w.Reason;
                    revoke.AddOption(label: label, value: w.Id.ToString("D"), description: desc);
                }

                c.WithActionRow(new[] { revoke });
            }

            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Voltar")
                    .WithCustomId($"warn:list:back:{page}")
                    .WithStyle(ButtonStyle.Secondary),
                new ButtonBuilder()
                    .WithLabel("Revogar todos")
                    .WithCustomId($"warn:revoke:all:{userId}:{page}")
                    .WithStyle(ButtonStyle.Danger)
                    .WithDisabled(warns.Count == 0)
            });
        });

        await component.UpdateAsync(msg =>
        {
            msg.Components = components;
            msg.Embeds = Array.Empty<Embed>();
        });
    }

    [ComponentInteraction("warn:list:back:*")]
    public async Task BackAsync(string pageRaw)
    {
        if (!int.TryParse(pageRaw, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await UpdateListAsync(page);
    }

    [ComponentInteraction("warn:revoke:select:*:*")]
    public async Task RevokeSelectedAsync(string userIdRaw, string pageRaw, string[] selections)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync("Nao consegui atualizar.", ephemeral: true);
            return;
        }

        if (!ulong.TryParse(userIdRaw, out var userId) || !int.TryParse(pageRaw, out var page))
        {
            await RespondAsync("Dados invalidos.", ephemeral: true);
            return;
        }

        if (selections.Length == 0 || !Guid.TryParse(selections[0], out var warnId))
        {
            await RespondAsync("Selecao invalida.", ephemeral: true);
            return;
        }

        await _warns.RevokeAsync(Context.Guild.Id, warnId, Context.User.Id);
        await AllWarnListLivePanel.TryRefreshAsync(Context.Client, _embeds, _warns, Context.Guild.Id);
        await RefreshUserWarnsAsync(component, userId, page);
    }

    [ComponentInteraction("warn:revoke:all:*:*")]
    public async Task RevokeAllAsync(string userIdRaw, string pageRaw)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync("Nao consegui atualizar.", ephemeral: true);
            return;
        }

        if (!ulong.TryParse(userIdRaw, out var userId) || !int.TryParse(pageRaw, out var page))
        {
            await RespondAsync("Dados invalidos.", ephemeral: true);
            return;
        }

        await _warns.RevokeAllAsync(Context.Guild.Id, userId, Context.User.Id);
        await AllWarnListLivePanel.TryRefreshAsync(Context.Client, _embeds, _warns, Context.Guild.Id);
        await RefreshUserWarnsAsync(component, userId, page);
    }

    private async Task RefreshUserWarnsAsync(SocketMessageComponent component, ulong userId, int page)
    {
        var warns = await _warns.GetActiveWarnsAsync(Context.Guild!.Id, userId);
        var embed = _embeds.CreateWarning("⚠️ Warns do usuario", BuildWarnDetails(userId, warns))
            .WithCurrentTimestamp();

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            if (warns.Count > 0)
            {
                var revoke = new SelectMenuBuilder()
                    .WithCustomId($"warn:revoke:select:{userId}:{page}")
                    .WithPlaceholder("Revogar um warn...")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                foreach (var w in warns.Take(25))
                {
                    var label = w.CreatedAtUtc.ToLocalTime().ToString("dd/MM HH:mm");
                    var desc = w.Reason.Length > 80 ? w.Reason[..80] + "..." : w.Reason;
                    revoke.AddOption(label: label, value: w.Id.ToString("D"), description: desc);
                }

                c.WithActionRow(new[] { revoke });
            }

            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Voltar")
                    .WithCustomId($"warn:list:back:{page}")
                    .WithStyle(ButtonStyle.Secondary),
                new ButtonBuilder()
                    .WithLabel("Revogar todos")
                    .WithCustomId($"warn:revoke:all:{userId}:{page}")
                    .WithStyle(ButtonStyle.Danger)
                    .WithDisabled(warns.Count == 0)
            });
        });

        await component.UpdateAsync(msg =>
        {
            msg.Components = components;
            msg.Embeds = Array.Empty<Embed>();
        });
    }

    private async Task UpdateListAsync(int page)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync("Nao consegui atualizar a lista.", ephemeral: true);
            return;
        }

        var all = await _warns.GetWarnedUsersAsync(Context.Guild.Id);
        if (all.Count == 0)
        {
            await component.UpdateAsync(msg =>
            {
                msg.Components = _embeds.BuildCv2Card(_embeds.CreateInfo("Warnlist", "Nenhum warn ativo."), _ => { });
                msg.Embeds = Array.Empty<Embed>();
            });
            return;
        }

        var totalPages = (int)Math.Ceiling(all.Count / (double)PageSize);
        var safePage = Math.Clamp(page, 1, Math.Max(totalPages, 1));
        var slice = all.Skip((safePage - 1) * PageSize).Take(PageSize).ToList();

        var embed = _embeds.CreateWarning("⚠️ Warnlist",
                $"Selecione um usuario para ver os warns.\nPagina {safePage}/{Math.Max(totalPages, 1)}")
            .WithCurrentTimestamp();

        var select = new SelectMenuBuilder()
            .WithCustomId($"warn:list:select:{safePage}")
            .WithPlaceholder("Selecione um usuario warnado...")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var u in slice)
        {
            var label = TryGetDisplayName((SocketGuild)Context.Guild, u.UserId);
            if (label.Length > 90)
            {
                label = label[..90];
            }

            select.AddOption(label: label, value: u.UserId.ToString(), description: $"{u.ActiveCount} warn(s) ativo(s)");
        }

        var comps = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[] { select });

            if (totalPages > 1)
            {
                c.WithActionRow(new[]
                {
                    new ButtonBuilder()
                        .WithLabel("◀")
                        .WithCustomId($"warn:list:prev:{safePage}")
                        .WithStyle(ButtonStyle.Secondary)
                        .WithDisabled(safePage <= 1),
                    new ButtonBuilder()
                        .WithLabel("▶")
                        .WithCustomId($"warn:list:next:{safePage}")
                        .WithStyle(ButtonStyle.Secondary)
                        .WithDisabled(safePage >= totalPages)
                });
            }
        });

        await component.UpdateAsync(msg =>
        {
            msg.Components = comps;
            msg.Embeds = Array.Empty<Embed>();
        });
    }

    private static string TryGetDisplayName(SocketGuild guild, ulong userId)
    {
        if (guild.GetUser(userId) is { } user)
        {
            return string.IsNullOrWhiteSpace(user.Nickname) ? user.Username : user.Nickname;
        }

        return $"User {userId}";
    }

    private static string BuildWarnDetails(ulong userId, IReadOnlyList<WarnEntry> warns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Usuario: <@{userId}>");
        sb.AppendLine($"Warns ativos: **{warns.Count}**");

        if (warns.Count == 0)
        {
            sb.AppendLine("\nNenhum warn ativo.");
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine();
        for (var i = 0; i < warns.Count; i++)
        {
            var w = warns[i];
            var when = w.CreatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
            sb.AppendLine($"**{i + 1}.** `{w.Id}`");
            sb.AppendLine($"- Quando: {when}");
            sb.AppendLine($"- Por: <@{w.DiscordModeratorId}>");
            sb.AppendLine($"- Motivo: {w.Reason}");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
