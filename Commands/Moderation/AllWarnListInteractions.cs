using System.Text;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Models.Entities;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Commands.Moderation;

public sealed class AllWarnListInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private const int PageSize = 10;
    private const int MaxReasonLength = 120;
    private readonly IWarnService _warns;
    private readonly EmbedHelper _embeds;

    public AllWarnListInteractions(IWarnService warns, EmbedHelper embeds)
    {
        _warns = warns;
        _embeds = embeds;
    }

    [ComponentInteraction("allwarn:open:*")]
    public async Task OpenAsync(string pageRaw)
    {
        if (!int.TryParse(pageRaw, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await RenderPrivatePageAsync(page, Context.User.Id, updateExistingMessage: false);
    }

    [ComponentInteraction("allwarn:prev:*:*")]
    public async Task PrevAsync(string pageRaw, string ownerUserIdRaw)
    {
        if (!ulong.TryParse(ownerUserIdRaw, out var ownerUserId) || ownerUserId != Context.User.Id)
        {
            await RespondAsync("Esse paginador nao e seu. Clique em `Navegar (privado)` no painel publico.", ephemeral: true);
            return;
        }

        if (!int.TryParse(pageRaw, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await RenderPrivatePageAsync(Math.Max(1, page - 1), ownerUserId, updateExistingMessage: true);
    }

    [ComponentInteraction("allwarn:next:*:*")]
    public async Task NextAsync(string pageRaw, string ownerUserIdRaw)
    {
        if (!ulong.TryParse(ownerUserIdRaw, out var ownerUserId) || ownerUserId != Context.User.Id)
        {
            await RespondAsync("Esse paginador nao e seu. Clique em `Navegar (privado)` no painel publico.", ephemeral: true);
            return;
        }

        if (!int.TryParse(pageRaw, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await RenderPrivatePageAsync(page + 1, ownerUserId, updateExistingMessage: true);
    }

    private async Task RenderPrivatePageAsync(int requestedPage, ulong ownerUserId, bool updateExistingMessage)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        var warns = await _warns.GetAllActiveWarnsAsync(Context.Guild.Id);
        if (warns.Count == 0)
        {
            var empty = _embeds.BuildCv2(_embeds.CreateInfo("Warns", "Nenhum warn ativo neste servidor."));
            if (updateExistingMessage && Context.Interaction is SocketMessageComponent component)
            {
                await component.UpdateAsync(msg =>
                {
                    msg.Components = empty;
                    msg.Embeds = Array.Empty<Embed>();
                });
            }
            else
            {
                await RespondAsync(components: empty, ephemeral: true);
            }

            return;
        }

        var totalPages = (int)Math.Ceiling(warns.Count / (double)PageSize);
        var safePage = Math.Clamp(requestedPage, 1, Math.Max(totalPages, 1));
        var startIndex = (safePage - 1) * PageSize;
        var pageWarns = warns.Skip(startIndex).Take(PageSize).ToList();

        var description = BuildDescription(pageWarns, startIndex);
        var embed = _embeds.CreateWarning("Navegacao privada - allwarnlist",
                $"Total de warns ativos: **{warns.Count}**\nPagina {safePage}/{Math.Max(totalPages, 1)}\n\n{description}")
            .WithCurrentTimestamp();

        var comps = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("◀")
                    .WithCustomId($"allwarn:prev:{safePage}:{ownerUserId}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage <= 1),
                new ButtonBuilder()
                    .WithLabel("▶")
                    .WithCustomId($"allwarn:next:{safePage}:{ownerUserId}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage >= totalPages)
            });
        });

        if (updateExistingMessage)
        {
            if (Context.Interaction is not SocketMessageComponent component)
            {
                await RespondAsync("Nao consegui atualizar a pagina.", ephemeral: true);
                return;
            }

            await component.UpdateAsync(msg =>
            {
                msg.Components = comps;
                msg.Embeds = Array.Empty<Embed>();
            });
            return;
        }

        await RespondAsync(components: comps, ephemeral: true);
    }

    private static string BuildDescription(IReadOnlyList<WarnEntry> warns, int startIndex)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < warns.Count; i++)
        {
            var warn = warns[i];
            var reason = NormalizeReason(warn.Reason);

            sb.AppendLine($"`#{startIndex + i + 1}` <@{warn.DiscordUserId}>");
            sb.AppendLine($"Motivo: {reason}");
            sb.AppendLine($"Moderador: <@{warn.DiscordModeratorId}> | Em: {warn.CreatedAtUtc:dd/MM/yyyy HH:mm} UTC");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string NormalizeReason(string reason)
    {
        var safe = string.IsNullOrWhiteSpace(reason) ? "(sem motivo)" : reason.Trim();
        if (safe.Length <= MaxReasonLength)
        {
            return safe;
        }

        return $"{safe[..MaxReasonLength]}...";
    }
}
