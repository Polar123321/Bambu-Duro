using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Commands.Moderation;

public sealed class StaffListInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IStaffApplicationStore _store;
    private readonly EmbedHelper _embeds;

    public StaffListInteractions(IStaffApplicationStore store, EmbedHelper embeds)
    {
        _store = store;
        _embeds = embeds;
    }

    [ComponentInteraction("staff:list:prev:*")]
    public async Task PrevAsync(string currentPage)
    {
        if (!int.TryParse(currentPage, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await UpdateAsync(Math.Max(1, page - 1));
    }

    [ComponentInteraction("staff:list:next:*")]
    public async Task NextAsync(string currentPage)
    {
        if (!int.TryParse(currentPage, out var page))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        await UpdateAsync(page + 1);
    }

    private async Task UpdateAsync(int page)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        if (Context.User is SocketGuildUser guildUser &&
            !guildUser.GuildPermissions.ManageGuild)
        {
            await RespondAsync("Permissao insuficiente.", ephemeral: true);
            return;
        }

        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync("Nao consegui atualizar a lista.", ephemeral: true);
            return;
        }

        const int pageSize = 5;
        var all = await _store.GetAllAsync(Context.Guild.Id);
        if (all.Count == 0)
        {
            await RespondAsync("Nenhuma candidatura encontrada.", ephemeral: true);
            return;
        }

        var totalPages = (int)Math.Ceiling(all.Count / (double)pageSize);
        var safePage = Math.Clamp(page, 1, Math.Max(totalPages, 1));

        var embed = _embeds.CreateInfo("Candidaturas a Staff",
                $"Pagina {safePage}/{Math.Max(totalPages, 1)}")
            .WithThumbnailUrl(Context.Client.CurrentUser.GetAvatarUrl());

        foreach (var entry in all.Skip((safePage - 1) * pageSize).Take(pageSize))
        {
            var shortMotivation = entry.Motivation.Length > 80
                ? entry.Motivation[..80] + "..."
                : entry.Motivation;

            embed.AddField($"{entry.Username} ({entry.UserId})",
                $"Enviado em: {entry.SubmittedAtUtc:dd/MM/yyyy HH:mm}\nMotivacao: {shortMotivation}");
        }

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("◀")
                    .WithCustomId($"staff:list:prev:{safePage}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage <= 1),
                new ButtonBuilder()
                    .WithLabel("▶")
                    .WithCustomId($"staff:list:next:{safePage}")
                    .WithStyle(ButtonStyle.Secondary)
                    .WithDisabled(safePage >= totalPages)
            });
        });

        await component.UpdateAsync(msg =>
        {
            msg.Components = components;
            msg.Embeds = Array.Empty<Embed>();
        });
    }
}
